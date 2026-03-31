#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>

#define TIME_BETWEEN_REPORTS 20000  // time in ms between sending attendance report
#define ACK_RESEND_TIME 2000        // time in ms between resending unacked messages
#define ACK_TIMEOUT 15000           // time in ms before giving up on sending a message. Keep this less than TIME_BETEEN_REPORTS to avoid problems
#define PERSON_DEBOUNCE_TIME 2500   // time in ms to wait before accepting another person
#define REAL_TIME_REPORTS true      // if true, send an MQTT message every time we see a person. If false, only send a report every TIME_BETWEEN_REPORTS

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

// stuff for people counting
bool personDetected = false;
unsigned int personCount, personCountMessage = 0;
const int trigPin = 2;  // these are for ultrasonic sensor
const int echoPin = 3;
#define SPEED_OF_SOUND 0.034
#define ULTRASONIC_TIMEOUT 10000  // time in MICROseconds before the sensor gives up
float sensorDistance;
#define DEBOUNCE_BUFFER_SIZE 30
bool debounced;
bool debounceHistory[DEBOUNCE_BUFFER_SIZE];
int debounceHistoryCount = 0;


void setup() {
  espClient.setInsecure();  // I think we need this because we aren't using https requests?

  // Set software serial baud to 115200 for debugging messages
  Serial.begin(115200);

  //sets pin modes for ultrasonic sensor
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);


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

  // Publish and subscribe to the relevant channels
  //client.publish(dataOutChannel, "esp32-client-1 connection test");
  client.subscribe(dataInChannel);
}

//Function that runs when we get a message through MQTT
void callback(char *topic, byte *payload, unsigned int length) {
  Serial.print("Message arrived in topic: ");
  Serial.println(topic);
  Serial.print("Message: ");
  char incomingMessage[] = "";
  for (int i = 0; i < length; i++) {
    //Serial.print((char)payload[i]);
    incomingMessage[i] = (char)payload[i];
  }
  Serial.println(incomingMessage);
  Serial.println("-----------------------");

  /*PUT LOGIC HERE FOR HANDLING COMMANDS FROM THE SERVER TO THE ESP32*/
  if (topic == "esp32CommandChannel") {
    if (incomingMessage == "A") {
      lastMessageAcked = true;
    }
  } else if (topic == "esp32DataChannel") {
    if (incomingMessage == "A") {
      lastMessageAcked = true;
    }
  }
}

float readSensorDistance() {
  long ultrasonicDuration;
  float distanceCm;
  // Clears the trigPin
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  // Sets the trigPin on HIGH state for 10 micro seconds
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(20);
  digitalWrite(trigPin, LOW);

  // Reads the echoPin, returns the sound wave travel time in microseconds
  ultrasonicDuration = pulseIn(echoPin, HIGH, ULTRASONIC_TIMEOUT);

  // Calculate the distance
  distanceCm = ultrasonicDuration * SPEED_OF_SOUND / 2;

  if (distanceCm < 2) distanceCm = 100;  // if less than 2 it was probably a misfire, set to 100 to avoid mistakes

  return distanceCm;
}

void sendAttendanceReport(bool thisIsAResend) {
  snprintf(message, sizeof(message), "%d:%d", personCount, (int)thisIsAResend);
  client.publish(dataOutChannel, message);
  if (thisIsAResend) Serial.print("[RESEND] ");
  Serial.print("Message sent. Contents: ");
  Serial.println(message);
}



void loop() {
  sensorDistance = readSensorDistance();
  Serial.println(sensorDistance);

  if (sensorDistance >= 25)  // if we detect a new person
  {

    lastPersonTime = millis();         // every time someone gets close, reset the timeout
    if (!personDetected && debounced)  // if this person is a new person (it has been long enough to assume it's another person)
    {
      personCount++;          // add one to the count
      personDetected = true;  // only do it once
      debounceHistoryCount = 0;
      Serial.print("Person Detected! New Count = ");
      Serial.println(personCount);
      if (REAL_TIME_REPORTS) sendAttendanceReport(false);
    } else if (!personDetected && !debounced) {
      debounceHistory[debounceHistoryCount] = true;
    }
    debounced = true;
    for (int i = 0; i < DEBOUNCE_BUFFER_SIZE; i++) {
      if (debounceHistory[i] == false) {
        debounced = false;
      }
    }

  } else if (sensorDistance <= 18 && ((millis() - lastPersonTime) > PERSON_DEBOUNCE_TIME)) {
    // after the person goes away and it has been long enough
    if (personDetected == true) Serial.println("Ready to accept new person.");
    personDetected = false;
  } else if (sensorDistance <= 18) {
    debounceHistory[debounceHistoryCount] = true;
  }

  debounceHistoryCount++;
  if (debounceHistoryCount >= DEBOUNCE_BUFFER_SIZE)
  {
    debounceHistoryCount = 0;
  }

  if (!REAL_TIME_REPORTS) {
    if (((millis() - lastSendTime) > TIME_BETWEEN_REPORTS) && lastMessageAcked == true) {  // regular attendance report
      lastSendTime = millis();                                                             // reset message timer
      messageAckTimer = millis();
      personCountMessage = personCount;  // save the person count to another variable so we can keep counting while we try to send
      sendAttendanceReport(false);
    }
  }


  client.loop();  // keep this: refreshes the MQTT link
  delay(20); // not all ultrasonic sensors can run fast, so this helps compatibility without harming functionality
}
