# OptumHsaSaveItExport
This project scrapes your own Optum HSA Save-It account using Selenium automation for Edge, outputting a csv file with all details

## Installation instructions
Copy the release locally and unzip to a folder

## Run instructions
1. Open a command prompt
2. Navigate to the folder and run the OptumHsaSaveItExport.exe

## Optional parameters
The following are optional parameters to customize the login handling
* manualLogin - you will be responsible for logging in and getting the browser to the optum site and HSA Save-it piggybank url
* basicLogin - you will be responsible for authenticating with SSO and clicking Yes/No to staying logged in

## Results
The results are saved in a csv in the same directory as the exe

There is one csv which is written at the very end: "OptumHsaSaveIt_yyyyMMdd-HHmmss.csv" and is newly created with each run.

Each attachment is also stored in the same directory with the format \[Date of service as yyyyMMdd\]_Optum ID_Health Plan ID_Service For_Vendor/Provider
