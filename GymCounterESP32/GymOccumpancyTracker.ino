#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>

#define TIME_BETWEEN_REPORTS 20000  // time in ms between sending attendance report
#define ACK_RESEND_TIME 2000        // time in ms between resending unacked messages
#define ACK_TIMEOUT 15000           // time in ms before giving up on sending a message. Keep this less than TIME_BETEEN_REPORTS to avoid problems
#define PERSON_DEBOUNCE_TIME 2000   // time in ms to wait before accepting another person

// WiFi
const char *ssid = "BYUI_Visitor";  // Wi-Fi ssid to connect to
const char *password = "";          // Wi-Fi password
WiFiClientSecure espClient;

// MQTT Broker and PubSubClient information
const char *mqtt_broker = "afa96227.ala.us-east-1.emqxsl.com";
const char *dataOutChannel = "esp32DataChannel";    // have website subscribe to this channel to be able to receive the stats
const char *dataInChannel = "esp32CommandChannel";  // This ESP32 will listen on this channel for any commands we want it to receive
const char *mqtt_username = "esp32";                // username and password were created inside the eqmx broker interface
const char *mqtt_password = "Pioneer47";
const int mqtt_port = 8883;
PubSubClient client(espClient);
bool lastMessageAcked = false;
char message[128];  // pubsubclient wants a definite size for the char array


// Timers
static unsigned long lastSendTime = 0;
static unsigned long messageAckTimer = 0;
static unsigned long lastPersonTime = 0;

// people counting stuff
bool personDetected = false;
unsigned int personCount, personCountMessage = 0;


void setup() {
  espClient.setInsecure();  // I think we need this because we aren't using https requests?

  // Set software serial baud to 115200 for debugging messages
  Serial.begin(115200);

  // Connect to a WiFi network
  WiFi.begin(ssid, password);
  //Retry until you have a connection
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print("Trying to connect to network: ");
    Serial.println(ssid);
  }
  Serial.println("Connected to the Wi-Fi network");

  //connect to the mqtt broker
  client.setServer(mqtt_broker, mqtt_port);
  client.setCallback(callback);  // <--- Run this function when we recieve a message from MQTT
  while (!client.connected()) {
    String client_id = "esp32-client-1";
    Serial.printf("The client %s connects to the public MQTT broker\n", client_id.c_str());
    if (client.connect(client_id.c_str(), mqtt_username, mqtt_password)) {
      Serial.println("MQTT broker connected");
    } else {
      Serial.print("failed with state ");
      Serial.println(client.state());
      delay(2000);
    }
  }

  // Publish and subscribe
  client.publish(dataOutChannel, "esp32-client-1 connection test");
  client.subscribe(dataInChannel);
}

void callback(char *topic, byte *payload, unsigned int length) {
  Serial.print("Message arrived in topic: ");
  Serial.println(topic);
  Serial.print("Message:");
  char incomingMessage[] = "";
  for (int i = 0; i < length; i++) {
    //Serial.print((char)payload[i]);
    incomingMessage[i] = (char)payload[i];
  }
  Serial.println(incomingMessage);
  Serial.println("-----------------------");

  /*PUT LOGIC HERE FOR HANDLING COMMANDS FROM THE SERVER TO THE ESP32*/
  if (topic == "esp32CommandChannel") {
    if (incomingMessage == "ACK") {
      lastMessageAcked = true;
    }
  }
  else if (topic == "esp32DataChannel") {
    if (incomingMessage == "ACK") {
      lastMessageAcked = true;
    }
  }
}

int readSensorDistance() {
  // put stuff here to read whatever sensor we are using and return an int
  return 3;
}

void sendAttendanceReport(bool thisIsAResend) {
  snprintf(message, sizeof(message), "%d:%d", (TIME_BETWEEN_REPORTS / 60000), personCount, (int)thisIsAResend);
  client.publish(dataOutChannel, message);
  Serial.print("Message sent. Contents: ");
  Serial.println(message);
}



void loop() {
  if (readSensorDistance() <= 4)  // if we detect a new person
  {
    Serial.println("Detected person with threshold.");
    lastPersonTime = millis();  // every time someone gets close, reset the timeout
    if (!personDetected)        // if this person is a new person (it has been long enough to assume it's another person)
    {
      personCount++;          // add one to the count
      personDetected = true;  // only do it once
      Serial.print("Person Detected! New Count = ");
      Serial.println(personCount);
    }

  } else if (readSensorDistance() >= 6 && ((millis() - lastPersonTime) > PERSON_DEBOUNCE_TIME)) {
    // after the person goes away and it has been long enough
    Serial.println("Ready to accept new person.");
    personDetected = false;
  }


  if (((millis() - lastSendTime) > TIME_BETWEEN_REPORTS) && lastMessageAcked == true) {  // regular attendance report
    lastSendTime = millis();                                                             // reset message timer
    messageAckTimer = millis();
    personCountMessage = personCount;  // save the person count to another variable so we can keep counting while we try to send
    sendAttendanceReport(false);
  }

  if ((millis() - messageAckTimer > ACK_RESEND_TIME) && !lastMessageAcked)  // if we haven't received an ACK
  {
    messageAckTimer = millis();  // reset the timer
    sendAttendanceReport(true);  // send another report, with resend flag enabled
  }

  client.loop();  // keep this: refreshes the MQTT link
}
