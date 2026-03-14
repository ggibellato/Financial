# Task

# 1 (done)
- At the Summary tab remove the Operations: and Credit Entries

- Make a new Balance that is Total Bought - Total Sold, arrange this three values on the same line

- Open a new session at the Summary that will present todays information for the Asset.
  - Look at Read FIIs current values to know how to get the value of the asset, as an extra information try to recover the date/time for the value
  - Make defensive code, because this can fail, but present information of the problem
  - Read the information only when the Asset is selected, once is ready don't try to read again, don't store this information
  - Add on this new summary session a refresh button that will read the informtion again
  

# 2 (done)
- Remove the Balance information
- Rename the Today label to Current  and give some emphasis to the label, nothing maxive
- If there is a avarege price for the Asset show the following
  - Total Current value = Current Value * quantity
  - Result % = (Total Current value/(Average price * quantity )) in green if positive rede if negativve

# 3 (done)
- Check if a Asset is reselect after first time load current value, if it is try to get the price again from the internet.
- If it is, it should NOT do that but use the previous value

# 4 (done)

- Lets add another total value with credits, this should be the Total Current Value + Total Credits
- Calculate the Result % for this total too

# 5 (done) 
- Add a copy to clipboard button on the side of the Asset Title normaly this type of icon is two square one over the other a little down to to right
- All the values : at their divisions should be aligned by the . even when they have different number of cases
The groups right now are : Total Bought and Total Credits
Current Value, Total Current Value and Total Current + Credits
Both Result %

# 6 (done)
On the Shares Dividen check tab there are two grids: Dividedn History and By Year

Make the line selection in this grids equal the ones at the Portfolio Navigator
- selection color
- text color when selected
- keep selected when click out of the grid

On the Read FIIs current values tab there is one grid: FIIs Current Prices

Make the line selection in this grids equal the ones at the Portfolio Navigator
- selection color
- text color when selected
- keep selected when click out of the grid


The line size or font size is not required to change, but colors it should change the Value columns should match the ones from the Values on Portifolio and on the By Year total use the same.


Finally rename the tab Read FIIs current values and its title to Read Assets current values


# 7
 At the Tabs Shares Dividend Check and Read Assets current values, the scroll bar should be at the grid only and not at the whole tab

 