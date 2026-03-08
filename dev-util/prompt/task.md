# Task

This file contains tasks to be executed.

1 - Remove the specflow code and package references from the whole solution.

2 - Rename JSONRepositor to LocalJSONRepository

3 - As the Repository design patter is used at this solution create a new GoogleDriveJSONRepository that will be used to access the the data.json from there.
The file path at gdrive is Pessoais/Gleison/Financeiros/data.json
The credentials to be used are the same the is sued by the project ImportGoogleSpreadSheets.

4 - create a configuration for the FinancialUI to have the google credencials setup and the file path 

5 - Update the ImportGoogleSpreadSheets project to have configruation for the credentials not use hardcode path to the file.