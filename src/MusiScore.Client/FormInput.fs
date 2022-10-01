namespace global

type FormInputValidation<'a> =
    | NotValidated
    | ValidationError of string
    | ValidationSuccess of 'a

type FormInput<'a> = {
    Text: string
    ValidationState: FormInputValidation<'a>
}
module FormInput =
    let empty =
        { Text = ""; ValidationState = NotValidated }
    let validated text value =
        { Text = text; ValidationState = ValidationSuccess value }
