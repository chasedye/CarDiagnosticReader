### Context:
This is my senior design project from UCF developed for Group-A Engineering.
The concept of this application is to allow a user to upload their car's binary file and 
display their information in the form of editable heat maps with 2D and 3D graphs. I led this project
with 3 other teammates and in the project we utilize a sql database to maintain all xml file definitions and  
created a custom graphical viewer to display the data as well as custom editable heat maps. 
Car binary files utilize an xml schema and place a xml rom id in a predefined address and this is used to query
the corresponding xml file from local sql db and parse the binary data.

##### readme from 2023
# CarDiagnosticReader
- click binary file for ECU Binary/Hex file
- click open for any XDF files if youre ecu file only has a correlating XDF
- click the file button for inputting another hex file
- We have a sqlite database housing xml files and their corresponding info we need, so only the XML files we have are included but can be added
by using DB Browser and adding corresponding info about the file and including the file itself in the application

  extra info:
- no installer included
- settings don't work but were added for the future of the app
- some xml files have extra info that are not included in the vast majority so slim chance of running into instances of not grabbing all the data
  
