# Task

This file contains tasks to be executed.

1 - Let's improve the the configuration DataJsonPath and FilePath.
  - The DataJsonPath rename to DataJsonFile and update the current configurations by add the file name data.json at the end.

2 - Fix the issue when accessing google drive to read data.json. The code is using the FilePath to find the file id, but the FilePath can include folders structure. It should first try to get the folder id recursively until get the last part that is the file name

3 - Fix the misspelling for the word Portfolio across the solution

4 - at the project ImportGoogleSpreadSheets ignore the data.json stored at the Google Drive

5 - check if the Window1.xaml at the project FinancialTools is used, if is not used delete it.

