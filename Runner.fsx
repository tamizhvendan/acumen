#r "packages/FAKE/tools/FakeLib.dll"
open Fake
#load "Fsi.fsx"
open Fsi
open System
#load "Steps.fsx"
open Steps

// ------------------
let pickRandomMessage list =
  let random = new System.Random()
  let index = random.Next(0, List.length list)   
  List.item index list   
let green = trace
let yellow = traceFAKE
let whitefn = printfn
let white = printf
let red = traceError
// ------------------


// Get Current Step From Env Variable
// Load Current Assert
// Open Current Assert
// Get Assert Info
// Print the assert and Wait for the file change
// If no change in MiniSuave keep waiting
// Load MiniSuave
// Open MiniSuave
// Execute the Assert 
//  True - 
//      Update the next step
//      Recurse to start
//  False -
//      Show the error
//      Recurse to file change


let assertExpression fsi content expected =
  match evalExpression fsi content with
  | Success value -> 
    match value with
    | Some v -> 
      let actual = v.ReflectionValue |> sprintf "%A" 
      if actual = expected then
       Success () 
      else
        sprintf "Expected %s but found %s" expected actual |> Error
    | None -> Success ()
  | Error msg -> Error msg

let runAssert fsi = function
| Compiler content -> evalInteraction fsi content
| Compiler2 (content, errorMsg) ->
  match evalInteraction fsi content with
  | Success v -> Success v
  | Error _ -> Error errorMsg
| Expression (content, expected) -> 
  assertExpression fsi content expected  
| Expression2 (content, expected, errMsg) ->
  match assertExpression fsi content expected with
  | Success _ -> Success ()
  | Error msg -> Error (sprintf "[Assertion (%s) failed] : %s" errMsg msg)


let personalise username (message : string) = 
  message.Replace("%s", username)

let inline goToNext () =
  printfn ""
  let rec prompt () =
    yellow "« Type next to continue »"
    white "λ "
    let command : string = Console.ReadLine()
    if String.Equals(command, "next", StringComparison.InvariantCultureIgnoreCase) then () 
    else prompt ()
  prompt ()
let rec runAsserts fsi xs =
  match xs with
  | [] -> Success ()
  | x :: xs ->
    match runAssert fsi x with
    | Success _ -> runAsserts fsi xs
    | Error msg -> Error msg
let executeStep username step =
  use fsi = fsi()
  match evalInteraction fsi """ #load "MiniSuave.fsx";;  """ with
  | Success _ -> 
    match evalInteraction fsi """ open MiniSuave;; """ with
    | Success _ -> 
      match runAsserts fsi step.Asserts with
      | Success _ -> 
        printfn ""
        pickRandomMessage step.Appreciations
        |> personalise username
        |> green  
        goToNext(); true
      | Error msg -> traceError msg; false
    | Error msg -> traceError msg; false
  | Error msg -> traceError msg; false
  
let execute stepCount username =
  steps |> List.item stepCount |> executeStep username

let printStep currentStep =
  Console.Clear()
  let step = steps |> List.item currentStep
  green <| sprintf "[Challenge %d of %d] %s" (currentStep+1) steps.Length step.Objective
  printfn ""
  printfn "[QuickHint] %s" step.QuickHint
  printfn ""

let totalSteps = steps.Length