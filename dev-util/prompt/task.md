# Task

1 - Create feature add, update and delete Operations
- Follow the architecture that is defined, the operations on domain need to be isolated from the UI and from Infrastructure
- Althought right now is not using a API to do this operations, it will in the future, therefore implementation of service and the use of CQRS pattern can be an option.
- Create tests 

2 - Allowed to entry information via the UI, (done)
   WHEN Add OPEN a form with default values, validate the information and have a confirmation button
   WHEN Update OPEN a form with values from the selected row, validate the information and have a confirmation button
   WHEN Delete OEPN a from with values from the selected row but can not be edit, and have a confirmation button

3 - The dates on the grids Operations and credits in fact all the dates should follow the windows configuration. My machine is UK date format but the grids are displaying the dates I believe in US format. Preferable use the format that add zeros when the day or the month has only one digit (done)

4 - Instead use buttons lets add icons at the lines to update or delete and just one for insert, it can be called new in this case