module ApplicativeDemo.Demo2

open Xunit
open TestHelper

type CreditCard = {
      Number: string
      Expiry: string
      Cvv: string }

let createCreditCard number expiry cvv = {
      Number = number
      Expiry = expiry
      Cvv = cvv }

// check 1: returns list of errors
let validateNumber number: Result<string, string list> =
    if String.length number < 2 then
        Error ["invalid credit card number"]
    else
        Ok number

// check 2: returns list of errors
let validateExpiry expiry: Result<string, string list> =
    if expiry = "invalid" then
        Error ["invalid expiry"]
    else
        Ok expiry

// check 3: returns list of errors
let validateCvv cvv: Result<string, string list> =
    if cvv = "invalid" then
        Error ["invalid cvv"]
    else
        Ok cvv

// Result<'a, 'b list> -> Result<('a -> 'c), 'b list> -> Result<'c, 'b list>
let apply a f =
    match f, a with
    | Ok g, Ok x -> g x |> Ok
    | Error e, Ok _ -> e |> Error
    | Ok _, Error e -> e |> Error
    | Error e1, Error e2 -> (e1 @ e2) |> Error // <- NOTE: errors are concatenated

// v1    
let validateCreditCardV1 card: Result<CreditCard, string list> =
    Ok createCreditCard
    |> apply (validateNumber card.Number)
    |> apply (validateExpiry card.Expiry)
    |> apply (validateCvv card.Cvv)

// v2
let (<!>) a b = Result.map a b

let (<*>) a b = apply b a

let validateCreditCardV2 card: Result<CreditCard, string list> =
    createCreditCard
    <!> validateNumber card.Number
    <*> validateExpiry card.Expiry
    <*> validateCvv card.Cvv
    
let validateCreditCard =
//    validateCreditCardV1
    validateCreditCardV2
    
[<Fact>]
let ``valid input returns credit card`` () =
    let card = createCreditCard "number" "expiry" "cvv"
    let expected = { Number = "number"; Expiry = "expiry"; Cvv = "cvv" }
    let result = validateCreditCard card
    checkOk result expected

[<Theory>]
[<InlineData("x", "expiry", "cvv", "invalid credit card number")>]
[<InlineData("123", "invalid", "cvv", "invalid expiry")>]
[<InlineData("123", "expiry", "invalid", "invalid cvv")>]
let ``single invalid input returns correct error message`` (number:string, expiry:string, cvv:string, expectedRaw:string) =
    let card = createCreditCard number expiry cvv
    let result = validateCreditCard card
    let expected = expectedRaw |> splitToList
    checkError result expected

[<Theory>]
[<InlineData("x", "invalid", "cvv", "invalid credit card number,invalid expiry")>]
[<InlineData("123", "invalid", "invalid", "invalid expiry,invalid cvv")>]
[<InlineData("x", "expiry", "invalid", "invalid credit card number,invalid cvv")>]
[<InlineData("x", "invalid", "invalid", "invalid credit card number,invalid expiry,invalid cvv")>]
let ``multiple invalid inputs returns correct error message`` (number:string, expiry:string, cvv:string, expectedRaw:string) =
    let card = createCreditCard number expiry cvv
    let result = validateCreditCard card
    let expected = expectedRaw |> splitToList
    checkError result expected
