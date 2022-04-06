# Funicular-Switch challenge

Welcome!
Try to refactor the SendFileToFtp program, without changing parameters of Main (FtpLib is an 'external' library and cannot be modified). 
Make Main return 0 in success and 1 in error case and print useful error information to console in case of invalid inputs or ftp errors.
Use the `Result` type from Funicular.Switch nuget package, introduction can be found on github (https://github.com/bluehands/Funicular-Switch).
Useful helper methods are `Map`, `Bind`, `Match` to build the pipeline, `Validate` to perform checks on input and `Try` to turn exceptions into Result(s).

Goal is to collect as much error information as possible and to respect *'Single Level of Abstraction'* and *'Do not Repeat Yourself'* principles.