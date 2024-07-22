# poleko2023

## About
This is a project that got awarded a 3rd place in a contest organised by a Polish company 
[POL-EKO](https://www.pol-eko.com.pl/en/). It's made using C#, making use of .NET 7 (latest version at the time 
of development) with WPF framework for UI and SQLite as the database solution.

It gets data from a proprietary device over HTTP and then displays them to end user as well as saves them to the 
database. User can view the data fetched from the device in real time or display the data saved in the database in form 
of a chart or a table by simply specifying the timespan.

It's translated to English and automatically selects the display language based on Windows language (Polish for Polish 
in Windows, English for every other language).

It's made to be easily expandable, so adding support for new types of devices is limited to extending the `Device` 
class and making a `DeviceControl` for it.
There's also no setup required, it's ready to run.

This repo also includes `TestingApi`, a little Node.js script emulating the behaviour of the device, so you can try
the app yourself.

## Running
To run the **monitoring app** you need to compile it using [.NET SDK](https://dotnet.microsoft.com/en-us/download) and 
then just simply run it.

To run the **testing API** you need to have [Node.js](https://nodejs.org/en/download/package-manager) installed, and then:
1. In `TestingApi`, run `npm i`.
2. Run the script using `node ./index.js`.

To add a device in the monitoring app, just press the `+` icon in the `Devices` panel and enter its details
(for the `TestingApi` running on the same PC as the app it's: Device IP: `127.0.0.1`, port: `56000`).

## Screenshots

<img src="https://i.imgur.com/jxxh3jv.png" alt="add device view" />
<img src="https://i.imgur.com/FFOB8Qg.png" alt="device view" />
<img src="https://i.imgur.com/CoxZOlR.png" alt="another device view" />
