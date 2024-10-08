import './assets/main.css'

import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { createPinia } from 'pinia'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { library } from '@fortawesome/fontawesome-svg-core'
import { faMusic, faStar, faPen, faTrash, faInfoCircle, faArrowUp, faArrowDown, faArrowLeft, faArrowRight, faMagnifyingGlassPlus, faMagnifyingGlassMinus } from '@fortawesome/free-solid-svg-icons'
import { faStar as farStar } from '@fortawesome/free-regular-svg-icons'

const pinia = createPinia()

library.add(faMusic, faStar, farStar, faPen, faTrash, faInfoCircle, faArrowUp, faArrowDown, faArrowLeft, faArrowRight, faMagnifyingGlassPlus, faMagnifyingGlassMinus)

createApp(App)
    .use(router)
    .use(pinia)
    .component('font-awesome-icon', FontAwesomeIcon)
    .mount('#app')
