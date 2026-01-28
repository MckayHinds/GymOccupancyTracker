# Gym Occupancy Tracker

This site is a prototype tracker that will show roughly how many people are at the HART gym at a given time base on input from a motion sensor on the door as it only opens for one person at a time.

# Team Members
Bracken Hibbert, Mckay Hinds, and Sterling Schurr

# Team Communication
As a team we are using a group text for communication outside of class. We start every class with a short team meeting to catch each other up on what we have been working on and that has been going well. 

## Instructions for Build/Use and Team Responsibilities

Steps to build and/or run the software:

1. Frontend: HTML, CSS, JavaScript (Bracken Hibbert)
    1. Set up main website for the program to run on. Make sure it is appealing and stylized, as well as add some functional slides through Javascript

2. Backend: C#, API, C++, Json, platformio (Mckay Hinds)
    1. Create system "brain" with C# to create where input from the website, and the ESP32 code can be processed and sent back out. Initialize the API so that C# is able to talk with Frontend and Hardware. Json is used to translate messages between both ends/ create a key for the API so it is more secure, and C++ creates a place for the ESP32 C3 to run it's code from Arduino.

3. Hardware: ESP32 C3, Arduino, C (Sterling Schurr)
    1. Secure the ESP32 C3 as well as create C code in Arduino

Instructions for using the software:

1. Go to website?
2. Use data on people in gym to decide when to attend.
3.

## Development Environment

To recreate the development environment, you need the following software and/or libraries with the specified versions:

* Need Github and vscode
* Connect to ou repository and connect code to website and hardware. 
*

## MQTT Protocol
The ESP32 speaks to an MQTT broker (basically a hardware backend) to get our messages from the ESP32 to the website backend. The ESP32 will send data through the "esp32DataChannel" channel and the backend will subscribe to that channel to receive the messages. The messages will be encoded with the information from the people entering the gym.

## Useful Websites to Learn More

I found these websites useful in developing this software:

* [Website Title] https://www.w3schools.com/CPP/default.asp 
