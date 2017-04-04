// Adafruit IO Publish Example
//
// Adafruit invests time and resources providing this open source code.
// Please support Adafruit and open source hardware by purchasing
// products from Adafruit!
//
// Written by Todd Treece for Adafruit Industries
// Copyright (c) 2016 Adafruit Industries
// Licensed under the MIT license.
//
// All text above must be included in any redistribution.

/************************** Configuration ***********************************/

// edit the config.h tab and enter your Adafruit IO credentials
// and any additional configuration needed for WiFi, cellular,
// or ethernet clients.
#include "config.h"
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <Adafruit_FeatherOLED.h>
#include <Adafruit_FeatherOLED_WiFi.h>
/************************ Example Starts Here *******************************/

//Adafruit_SSD1306 display = Adafruit_SSD1306();
Adafruit_FeatherOLED_WiFi display = Adafruit_FeatherOLED_WiFi();

#if (SSD1306_LCDHEIGHT != 32)
 #error("Height incorrect, please fix Adafruit_SSD1306.h!");
#endif

#define DOORSENSOR1_PIN   12
#define DOORSENSOR2_PIN   13
#define RELAY1_PIN        15
#define RELAY2_PIN        16
#define BUTTON_PRESSED    false
#define BUTTON_NOTPRESSED true

bool current = false;
bool last = false;
int sensorSampleCounter = 0;

// set up the IO feed
AdafruitIO_Feed *garage_door_right_status = io.feed("garage-door-right-status");
AdafruitIO_Feed *garage_door_right_button = io.feed("garage-door-right-button");
AdafruitIO_Feed *garage_door_left_status = io.feed("garage-door-left-status");
AdafruitIO_Feed *garage_door_left_button = io.feed("garage-door-left-button");

void setup() {
  pinMode(BUTTON_C, INPUT);
  pinMode(LED, OUTPUT);
  pinMode(DOORSENSOR1_PIN, INPUT_PULLUP);
  pinMode(DOORSENSOR2_PIN, INPUT_PULLUP);
  pinMode(RELAY1_PIN, OUTPUT);
  pinMode(RELAY2_PIN, OUTPUT);

  // start the serial connection
  Serial.begin(115200);
  
  // by default, we'll generate the high voltage from the 3.3v line internally! (neat!)
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);  // initialize with the I2C addr 0x3C (for the 128x32)
  
  // wait for serial monitor to open
  while(! Serial);

  digitalWrite(RELAY1_PIN, HIGH);
  digitalWrite(RELAY2_PIN, HIGH);
  
  // Show image buffer on the display hardware.
  // Since the buffer is intialized with an Adafruit splashscreen
  // internally, this will display the splashscreen.
  display.display();
  delay(1000);

  // Clear the buffer.
  display.clearDisplay();
  display.display();

  display.setTextSize(1);
  display.setTextColor(WHITE);
  display.setCursor(0,0);
  
  // connect to io.adafruit.com
  DisplayText("Connecting to Adafruit IO");
   
  io.connect();

  // setup feed data handlers
  garage_door_right_button->onMessage(onGarageDoorRightButtonMessage);
  garage_door_left_button->onMessage(onGarageDoorLeftButtonMessage);
  
  // wait for a connection
  while(io.status() < AIO_CONNECTED) {
    DisplayText(".");
    delay(500);
  }

  // we are connected
  Serial.println();
  Serial.println(io.statusText());
  display.println(io.statusText());
  display.display();

  uint32_t ipAddress = WiFi.localIP();
  Serial.println(ipAddress);
  display.println(ipAddress);
  display.display();

  display.clearDisplay();
  display.display();
  display.setConnected(true);
  display.setRSSI(WiFi.RSSI());
  display.setRSSIIcon(true);
  display.setRSSIVisible(true);
  display.setIPAddress(ipAddress);
  display.setIPAddressVisible(true);
  display.setBatteryVisible(false);
  display.setBatteryIcon(false);
  display.refreshIcons();
  display.clearMsgArea();
  display.display();
}

void loop() {
  // keep the client connected to io.adafruit.com, and process any incoming data.
  io.run();

  if(sensorSampleCounter == 4) {
    display.setRSSI(WiFi.RSSI());
    display.refreshIcons();
    display.display();
    Serial.print("Reading sensors...");
    int rightDoorSensor = digitalRead(DOORSENSOR1_PIN);
    int leftDoorSensor = digitalRead(DOORSENSOR2_PIN);
    Serial.print("RIGHT door is ");
    Serial.print(rightDoorSensor);
    Serial.print(", LEFT door is ");
    Serial.println(leftDoorSensor);

    if(rightDoorSensor == HIGH) {
      garage_door_right_status->save("open");
    } else {
      garage_door_right_status->save("closed");
    }

    if(leftDoorSensor == HIGH) {
      garage_door_left_status->save("open");
    } else {
      garage_door_left_status->save("closed");
    }
    
    sensorSampleCounter = 0;
  }

  sensorSampleCounter++;
  delay(500);
}

void onGarageDoorRightButtonMessage(AdafruitIO_Data *data) {
  bool ioData = data->toPinLevel();

  display.clearMsgArea();
  
  char message[64] = "-> ";
  
  if(ioData == HIGH) {
    strcat(message, "\"HIGH\"");
  } else {
    strcat(message, "\"LOW\"");
  }
  
  strcat(message, " for RIGHT door btn\0");

  Serial.println(message);
  display.println(message);
  display.display();

  digitalWrite(LED, !ioData);
  digitalWrite(RELAY1_PIN, !ioData);
}

void onGarageDoorLeftButtonMessage(AdafruitIO_Data *data) {
  bool ioData = data->toPinLevel();

  display.clearMsgArea();
  display.display();
  
  char message[64] = "-> ";
  
  if(ioData == HIGH) {
    strcat(message, "\"HIGH\"");
  } else {
    strcat(message, "\"LOW\"");
  }
  
  strcat(message, " for LEFT door btn\0");

  Serial.println(message);
  display.println(message);
  display.display();

  digitalWrite(LED, !ioData);
  digitalWrite(RELAY2_PIN, !ioData);
}

void DisplayText(const char* text) {
  Serial.print(text);
  display.print(text);
  display.display();
}

