# poleko2023

## About
This is a project that got awarded a 3rd place in a contest organised by a Polish company 
[POL-EKO](https://www.pol-eko.com.pl/en/). It's made using C#, making use of .NET 7.0 with WPF framework for UI and 
SQLite as the database solution.

It gets data from a proprietary device over HTTP and then displays them to end user as well as saves them to the 
database. User can view the data fetched from the device in real time as well as display the data saved in the database
by just simply specifying the timespan as both chart and table. Any device or network errors are clearly displayed to 
the user and there's a reconnection timeout. You can monitor multiple devices at the same time, which makes use of
WPF-provided tabs.

It's translated to English and automatically selects the display language based on Windows' language (Polish for Polish 
in Windows, English for every other language).

It's made to be easily expandable, so adding support for new types of devices is limited to extending the `Device` 
class and making a `DeviceControl` for it.

This repo also includes `TestingApi`, a little Node.js script emulating the behaviour of the device, so you can try
the app yourself.

## Screenshots

[[https://i.imgur.com/FFOB8Qg.png|alt=deviceview]]
[[https://i.imgur.com/CoxZOlR.png|alt=anotherdeviceview]]