<script setup lang="ts">
import { computed, ref } from 'vue'
import type { EditablePrintConfigRule, PrintConfigRuleDto, PrintConfigRuleInputs, PrintConfigRuleSaveError } from './PrintConfigTypes'
import { uiFetchAuthorized } from './UIFetch'
import _ from 'lodash'
import ErrorWithRetry from './ErrorWithRetry.vue'
import LoadingBar from './LoadingBar.vue'
import draggable from 'vuedraggable'

const props = defineProps<{
  printConfigRulesUrl: string
  /** print configs that rules can point to, the first one is used if no rule matches */
  printConfigs: { key: string, name: string }[]
}>()

/** only fill in the page size, rules just store width and height */
const pageSizePresets = [
  { name: 'A3', widthMm: 297, heightMm: 420 },
  { name: 'A4', widthMm: 210, heightMm: 297 },
  { name: 'A5', widthMm: 148, heightMm: 210 },
  { name: 'A6', widthMm: 105, heightMm: 148 },
  { name: 'B4', widthMm: 250, heightMm: 353 },
  { name: 'B5', widthMm: 176, heightMm: 250 },
  { name: 'Letter', widthMm: 216, heightMm: 279 },
  { name: 'Legal', widthMm: 216, heightMm: 356 },
  { name: 'Tabloid', widthMm: 279, heightMm: 432 },
]

/** the server also ignores the orientation when comparing page sizes */
const getPageSizePreset = (rule: EditablePrintConfigRule) => {
  if (rule.pageWidthMm === '' && rule.pageHeightMm === '') return ''
  const preset = pageSizePresets.find(v =>
    (v.widthMm === rule.pageWidthMm && v.heightMm === rule.pageHeightMm) ||
    (v.widthMm === rule.pageHeightMm && v.heightMm === rule.pageWidthMm))
  return preset?.name ?? 'custom'
}

/** width and height of a preset in the order that fits the orientation of the rule */
const getOrientedPresetSize = (rule: EditablePrintConfigRule, preset: typeof pageSizePresets[number]) => {
  const shortSide = Math.min(preset.widthMm, preset.heightMm)
  const longSide = Math.max(preset.widthMm, preset.heightMm)
  return rule.orientation === 'landscape'
    ? { widthMm: longSide, heightMm: shortSide }
    : { widthMm: shortSide, heightMm: longSide }
}

const applyPageSizePreset = (rule: EditablePrintConfigRule, presetName: string) => {
  const preset = pageSizePresets.find(v => v.name === presetName)
  const size = preset !== undefined ? getOrientedPresetSize(rule, preset) : undefined
  rule.pageWidthMm = size?.widthMm ?? ''
  rule.pageHeightMm = size?.heightMm ?? ''
}

/**
 * Page sizes are compared independent of the orientation, but showing e.g. 210 x 297 together with
 * landscape would be confusing, so width and height are swapped to fit the orientation.
 */
const orientPageSize = (rule: EditablePrintConfigRule) => {
  if (rule.pageWidthMm === '' || rule.pageHeightMm === '') return

  const isLandscape = rule.pageWidthMm > rule.pageHeightMm
  if ((rule.orientation === 'landscape' && !isLandscape) || (rule.orientation === 'portrait' && isLandscape)) {
    [rule.pageWidthMm, rule.pageHeightMm] = [rule.pageHeightMm, rule.pageWidthMm]
  }
}

const orientationOptions = [
  { key: '', value: 'Beliebige Ausrichtung' },
  { key: 'portrait', value: 'Hochformat' },
  { key: 'landscape', value: 'Querformat' },
]

let nextRuleId = 1
const rules = ref<EditablePrintConfigRule[]>()
const loadedData = ref<PrintConfigRuleInputs[]>()
const isLoading = ref(false)
const hasLoadingFailed = ref(false)
const isSaving = ref(false)
const hasSavingFailed = ref(false)
const saveError = ref<string>()

const getInputs = (rule: EditablePrintConfigRule) : PrintConfigRuleInputs => ({
  minPageCount: rule.minPageCount,
  maxPageCount: rule.maxPageCount,
  pageWidthMm: rule.pageWidthMm,
  pageHeightMm: rule.pageHeightMm,
  orientation: rule.orientation,
  printConfig: rule.printConfig,
})

const load = async () => {
  const result = await uiFetchAuthorized(isLoading, hasLoadingFailed, props.printConfigRulesUrl)
  if (!result.succeeded) return

  const loadedRules = await result.response.json() as PrintConfigRuleDto[]
  rules.value = loadedRules.map((v) : EditablePrintConfigRule => ({
    id: `${nextRuleId++}`,
    minPageCount: v.minPageCount,
    maxPageCount: v.maxPageCount ?? '',
    pageWidthMm: v.pageWidthMm ?? '',
    pageHeightMm: v.pageHeightMm ?? '',
    orientation: v.orientation ?? '',
    printConfig: v.printConfig,
    isNew: false,
    delete: false,
    errors: [],
  }))
  loadedData.value = rules.value.map(getInputs)
}
load()

const addRule = () => {
  if (rules.value === undefined) return

  rules.value.push({
    id: `${nextRuleId++}`,
    minPageCount: 1,
    maxPageCount: '',
    pageWidthMm: '',
    pageHeightMm: '',
    orientation: '',
    printConfig: '',
    isNew: true,
    delete: false,
    errors: [],
  })
}

const toggleDeleteRule = (rule: EditablePrintConfigRule) => {
  if (rules.value === undefined) return

  if (rule.isNew) {
    rules.value.splice(rules.value.indexOf(rule), 1)
  }
  else {
    rule.delete = !rule.delete
    rule.errors = []
  }
}

/** rules in evaluation order, without the ones marked for deletion */
const activeRules = computed(() => rules.value?.filter(v => !v.delete) ?? [])

const isUnknownPrintConfig = (rule: EditablePrintConfigRule) =>
  rule.printConfig !== '' && !props.printConfigs.some(v => v.key === rule.printConfig)

const defaultPrintConfig = computed(() => props.printConfigs[0])

const canSave = computed(() =>
  rules.value !== undefined && !_.isEqual(loadedData.value, activeRules.value.map(getInputs)))

const validate = (rule: EditablePrintConfigRule) => {
  const isPositiveInteger = (v: number | '') => typeof v === 'number' && Number.isInteger(v) && v >= 1
  const isValidPageCount = isPositiveInteger
  rule.errors = [
    ...(!isValidPageCount(rule.minPageCount) || (rule.maxPageCount !== '' && !isValidPageCount(rule.maxPageCount))
      ? ['Die Seitenanzahl muss eine ganze Zahl größer als 0 sein.']
      : rule.minPageCount !== '' && rule.maxPageCount !== '' && rule.minPageCount > rule.maxPageCount
        ? ['Die minimale Seitenanzahl darf nicht größer als die maximale sein.']
        : []),
    ...((rule.pageWidthMm === '') !== (rule.pageHeightMm === '')
      ? ['Bitte geben Sie Breite und Höhe der Seite ein.']
      : rule.pageWidthMm !== '' && !(isPositiveInteger(rule.pageWidthMm) && isPositiveInteger(rule.pageHeightMm))
        ? ['Breite und Höhe der Seite müssen ganze Zahlen größer als 0 sein.']
        : []),
    ...(rule.printConfig === '' ? ['Bitte wählen Sie eine Druckeinstellung aus.'] : []),
    ...(isUnknownPrintConfig(rule) ? ['Die Druckeinstellung existiert nicht mehr.'] : []),
  ]
  return rule.errors.length === 0
}

const handleSaveErrors = (errors: PrintConfigRuleSaveError[]) => {
  if (rules.value === undefined) return

  for (const error of errors) {
    const messages = error.errors.map(v => {
      switch (v) {
        case 'InvalidPageCount': return 'Die Seitenanzahl muss eine ganze Zahl größer als 0 sein.'
        case 'InvalidPageCountRange': return 'Die minimale Seitenanzahl darf nicht größer als die maximale sein.'
        case 'InvalidPageSize': return 'Breite und Höhe der Seite müssen ganze Zahlen größer als 0 sein.'
        case 'IncompletePageSize': return 'Bitte geben Sie Breite und Höhe der Seite ein.'
        case 'InvalidOrientation': return 'Ungültige Ausrichtung.'
        case 'InvalidKey': return 'Bitte wählen Sie eine Druckeinstellung aus.'
        case 'UnknownPrintConfig': return 'Mindestens eine Druckeinstellung existiert nicht mehr.'
      }
    })
    // indices refer to the rules that were sent
    const rule = error.index !== null ? activeRules.value[error.index] : undefined
    if (rule !== undefined) {
      rule.errors = messages
    }
    else {
      saveError.value = messages.join(' ')
    }
  }
}

const save = async () => {
  if (rules.value === undefined) return
  saveError.value = undefined
  if (!activeRules.value.map(validate).every(v => v)) return

  const data = activeRules.value.map((v) : PrintConfigRuleDto => ({
    // validated before, so it can't be empty
    minPageCount: v.minPageCount as number,
    maxPageCount: v.maxPageCount === '' ? null : v.maxPageCount,
    pageWidthMm: v.pageWidthMm === '' ? null : v.pageWidthMm,
    pageHeightMm: v.pageHeightMm === '' ? null : v.pageHeightMm,
    orientation: v.orientation === '' ? null : v.orientation,
    printConfig: v.printConfig,
  }))
  const result = await uiFetchAuthorized(isSaving, hasSavingFailed, props.printConfigRulesUrl, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  })
  if (result.succeeded) {
    rules.value = activeRules.value
    for (const rule of rules.value) {
      rule.isNew = false
    }
    loadedData.value = rules.value.map(getInputs)
  }
  else if (result.response !== undefined && result.response.status === 400) {
    handleSaveErrors(await result.response.json() as PrintConfigRuleSaveError[])
  }
}

defineExpose({ canSave, save, load })
</script>

<template>
  <h3 class="mt-4 text-xl small-caps">Druckeinstellung beim Import automatisch wählen</h3>
  <p class="text-sm text-slate-700">Die Regeln werden von oben nach unten geprüft, die erste passende Regel bestimmt die Druckeinstellung.</p>
  <p class="text-sm text-slate-700">Das Format muss auf allen Seiten auf ±5 mm übereinstimmen, unabhängig von der Ausrichtung.</p>
  <LoadingBar v-if="isLoading" />
  <ErrorWithRetry v-else-if="hasLoadingFailed" type="inline" @retry="load">Fehler beim Laden der Regeln.</ErrorWithRetry>
  <div v-else-if="rules !== undefined" class="mt-2 flex flex-col gap-2" :class="{ 'opacity-50': isSaving }">
    <draggable v-model="rules" item-key="id" animation="150" tag="ol" handle=".rule-handle" class="flex flex-col gap-2">
      <template #item="{ element: rule } : { element: EditablePrintConfigRule }">
        <li>
          <fieldset :disabled="isSaving" class="flex items-start gap-2 border rounded-sm p-2">
            <div class="w-10 shrink-0 self-stretch flex flex-col items-center justify-center gap-2">
              <button class="btn" :class="{ 'btn-solid btn-red': rule.delete }" title="Regel löschen" @click="toggleDeleteRule(rule)">
                <font-awesome-icon :icon="['fas', 'trash-can']" />
              </button>
              <div class="w-6 text-center" :class="rule.delete ? 'opacity-50' : 'rule-handle cursor-grab'">
                <font-awesome-icon :icon="['fas', 'up-down-left-right']" />
              </div>
            </div>
            <fieldset :disabled="rule.delete" class="flex flex-col gap-2" :class="{ 'opacity-50': rule.delete }">
              <div class="flex flex-wrap items-center gap-2">
                <span class="w-16 text-sm">Seiten</span>
                <span class="text-sm">von</span>
                <input type="number" min="1" required v-model="rule.minPageCount" class="input-text min-w-20! w-20" />
                <span class="text-sm">bis</span>
                <input type="number" min="1" placeholder="∞" v-model="rule.maxPageCount" class="input-text min-w-20! w-20" />
              </div>
              <div class="flex flex-wrap items-center gap-2">
                <span class="w-16 text-sm">Format</span>
                <select class="input-select min-w-0! w-52" :value="getPageSizePreset(rule)" @change="applyPageSizePreset(rule, ($event.target as HTMLSelectElement).value)">
                  <option value="">Beliebiges Format</option>
                  <option v-for="preset in pageSizePresets" :key="preset.name" :value="preset.name">{{ preset.name }} ({{ getOrientedPresetSize(rule, preset).widthMm }} × {{ getOrientedPresetSize(rule, preset).heightMm }} mm)</option>
                  <option value="custom" disabled>Eigenes Format</option>
                </select>
                <input type="number" min="1" placeholder="Breite" v-model="rule.pageWidthMm" @change="orientPageSize(rule)" class="input-text min-w-20! w-20" />
                <span class="text-sm">×</span>
                <input type="number" min="1" placeholder="Höhe" v-model="rule.pageHeightMm" @change="orientPageSize(rule)" class="input-text min-w-20! w-20" />
                <span class="text-sm">mm</span>
                <select class="input-select min-w-0! w-52" v-model="rule.orientation" @change="orientPageSize(rule)">
                  <option v-for="option in orientationOptions" :key="option.key" :value="option.key">{{ option.value }}</option>
                </select>
              </div>
              <div class="flex flex-wrap items-center gap-2">
                <span class="w-16 text-sm">Druck</span>
                <select class="input-select min-w-0! w-64" :class="{ 'border-musi-red!': isUnknownPrintConfig(rule) }" v-model="rule.printConfig">
                  <option disabled value="">Druckeinstellung auswählen</option>
                  <option v-for="printConfig in printConfigs" :key="printConfig.key" :value="printConfig.key">{{ printConfig.name }}</option>
                </select>
              </div>
              <ul v-if="rule.errors.length > 0" class="flex flex-col gap-2">
                <li v-for="error in rule.errors" :key="error" class="text-sm text-musi-red">{{ error }}</li>
              </ul>
            </fieldset>
          </fieldset>
        </li>
      </template>
    </draggable>
    <div class="flex items-center gap-2 px-2 text-sm">
      <span class="w-10 shrink-0"></span>
      <span class="w-16">Sonst</span>
      <font-awesome-icon :icon="['fas', 'arrow-right']" />
      <span v-if="defaultPrintConfig !== undefined">{{ defaultPrintConfig.name }} <span class="text-slate-500">(erste Druckeinstellung)</span></span>
      <span v-else class="text-slate-500">keine Druckeinstellung vorhanden</span>
    </div>
    <button class="!flex items-center gap-2 btn btn-green btn-solid self-start px-8! py-4!" :disabled="isSaving" @click="addRule">
      <font-awesome-icon :icon="['fas', 'plus']" />
      <span>Neue Regel</span>
    </button>
    <span v-if="saveError !== undefined" class="text-sm text-musi-red">{{ saveError }}</span>
    <span v-else-if="hasSavingFailed && rules.every(v => v.errors.length === 0)" class="text-sm text-musi-red">Fehler beim Speichern der Regeln.</span>
  </div>
</template>
