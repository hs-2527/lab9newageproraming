
open System
open Microsoft.Data.Sqlite

// Simple student record used in our domain model
type Student = {
    Id: int
    Name: string
    Course: string
    Marks: int
}

let connectionString = "Data Source=students.db"

let createTable () =
    use connection = new SqliteConnection(connectionString)
    connection.Open()
    use command = connection.CreateCommand()
    command.CommandText <-
        """CREATE TABLE IF NOT EXISTS Students (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Course TEXT NOT NULL,
            Marks INTEGER NOT NULL
        )"""
    command.ExecuteNonQuery() |> ignore

// Add a new student
let addStudent (student: Student) =
    async {
        use connection = new SqliteConnection(connectionString)
        connection.Open()
        use command = connection.CreateCommand()
        command.CommandText <-
            "INSERT INTO Students (Id, Name, Course, Marks)
             VALUES ($id, $name, $course, $marks)"
        command.Parameters.AddWithValue("$id", student.Id) |> ignore
        command.Parameters.AddWithValue("$name", student.Name) |> ignore
        command.Parameters.AddWithValue("$course", student.Course) |> ignore
        command.Parameters.AddWithValue("$marks", student.Marks) |> ignore
        let result = command.ExecuteNonQuery()
        return result > 0
    }

// Read all students from database
let getStudents () =
    async {
        use connection = new SqliteConnection(connectionString)
        connection.Open()
        use command = connection.CreateCommand()
        command.CommandText <- "SELECT Id, Name, Course, Marks FROM Students ORDER BY Id"
        use reader = command.ExecuteReader()
        let students = ResizeArray<Student>()
        while reader.Read() do
            students.Add({
                Id = reader.GetInt32(0)
                Name = reader.GetString(1)
                Course = reader.GetString(2)
                Marks = reader.GetInt32(3)
            })
        return students |> Seq.toList
    }

// Update student details
let updateStudent id name course marks =
    async {
        use connection = new SqliteConnection(connectionString)
        connection.Open()
        use command = connection.CreateCommand()
        command.CommandText <-
            "UPDATE Students SET Name=$name, Course=$course, Marks=$marks WHERE Id=$id"
        command.Parameters.AddWithValue("$id", id) |> ignore
        command.Parameters.AddWithValue("$name", name) |> ignore
        command.Parameters.AddWithValue("$course", course) |> ignore
        command.Parameters.AddWithValue("$marks", marks) |> ignore
        return command.ExecuteNonQuery() > 0
    }

// Delete a student
let deleteStudent id =
    async {
        use connection = new SqliteConnection(connectionString)
        connection.Open()
        use command = connection.CreateCommand()
        command.CommandText <- "DELETE FROM Students WHERE Id=$id"
        command.Parameters.AddWithValue("$id", id) |> ignore
        return command.ExecuteNonQuery() > 0
    }

// Pattern matching example
let resultMessage marks =
    match marks with
    | m when m >= 80 -> "Excellent"
    | m when m >= 60 -> "Good"
    | m when m >= 40 -> "Pass"
    | _ -> "Needs Improvement"

let readInt prompt =
    printf "%s" prompt
    match Int32.TryParse(Console.ReadLine()) with
    | true, value -> Some value
    | false, _ -> None

let readText prompt =
    printf "%s" prompt
    Console.ReadLine()

let showStudents () =
    let students = getStudents () |> Async.RunSynchronously
    if List.isEmpty students then
        printfn "No student records found."
    else
        // Higher-order functions: map and filter
        let passedStudents = students |> List.filter (fun s -> s.Marks >= 40)
        let displayLines =
            passedStudents
            |> List.map (fun s ->
                sprintf "ID: %d | Name: %s | Course: %s | Marks: %d | Result: %s"
                    s.Id s.Name s.Course s.Marks (resultMessage s.Marks))
        displayLines |> List.iter (printfn "%s")

let rec menu () =
    printfn ""
    printfn "===== Student Management System ====="
    printfn "1. Add student"
    printfn "2. View students"
    printfn "3. Update student"
    printfn "4. Delete student"
    printfn "5. Show passed students"
    printfn "6. Exit"
    match readInt "Enter your choice: " with
    | Some 1 ->
        match readInt "Enter student ID: " with
        | Some id ->
            let name = readText "Enter student name: "
            let course = readText "Enter course: "
            match readInt "Enter marks: " with
            | Some marks when marks >= 0 && marks <= 100 ->
                let student = { Id = id; Name = name; Course = course; Marks = marks }
                try
                    let added = addStudent student |> Async.RunSynchronously
                    if added then printfn "Student added successfully."
                    else printfn "Student was not added."
                with ex -> printfn "Error: %s" ex.Message
            | _ -> printfn "Please enter marks between 0 and 100."
        | None -> printfn "Invalid student ID."
        menu ()
    | Some 2 ->
        showStudents ()
        menu ()
    | Some 3 ->
        match readInt "Enter student ID to update: " with
        | Some id ->
            let name = readText "Enter new name: "
            let course = readText "Enter new course: "
            match readInt "Enter new marks: " with
            | Some marks when marks >= 0 && marks <= 100 ->
                let updated = updateStudent id name course marks |> Async.RunSynchronously
                if updated then printfn "Student updated successfully."
                else printfn "Student ID not found."
            | _ -> printfn "Invalid marks."
        | None -> printfn "Invalid student ID."
        menu ()
    | Some 4 ->
        match readInt "Enter student ID to delete: " with
        | Some id ->
            let deleted = deleteStudent id |> Async.RunSynchronously
            if deleted then printfn "Student deleted successfully."
            else printfn "Student ID not found."
        | None -> printfn "Invalid student ID."
        menu ()
    | Some 5 ->
        let students = getStudents () |> Async.RunSynchronously
        let passed = students |> List.filter (fun s -> s.Marks >= 40)
        let names = passed |> List.map (fun s -> s.Name)
        printfn "Passed students: %A" names
        menu ()
    | Some 6 -> printfn "Exiting Student Management System."
    | _ ->
        printfn "Invalid choice. Please try again."
        menu ()

[<EntryPoint>]
let main _ =
    try
        createTable ()
        menu ()
        0
    with ex ->
        printfn "Application error: %s" ex.Message
        1
