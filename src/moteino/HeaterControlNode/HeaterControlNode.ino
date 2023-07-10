#include <RFM69.h>
#include <RFM69_ATC.h>
#include "LowPower.h"

//#define DEBUG
#include "DebugUtils.h"

// Device
//#define FREQUENCY   RF69_868MHZ
#define FREQUENCY     RF69_915MHZ
#define ENCRYPT_KEY   "0123456789abcdef" // 16 bytes
#define IS_RFM69HW
#define ENABLE_ATC // Auto Transmission Control

// Pins
#define LED           9
#define BTN_UP        16
#define BTN_DOWN      14
#define BTN_POW       15
#define INT_RADIO     2 // interrupt

// Network
#define NODE_ID       2    // Unique
#define NETWORK_ID    100  // Same
#define GATEWAY_ID    1
#define SERIAL_BAUD   115200

// Globals
#ifdef ENABLE_ATC
  RFM69_ATC radio;
#else
  RFM69 radio;
#endif

const char REQUEST = 0x2;
const unsigned long TIMEOUT = 40; // ms

void setup()
{
  initPins();

  Serial.begin(SERIAL_BAUD);
  radio.initialize(FREQUENCY, NODE_ID, NETWORK_ID);

#ifdef IS_RFM69HW
  radio.setHighPower();
#endif
  radio.encrypt(ENCRYPT_KEY);

#ifdef ENABLE_ATC
  radio.enableAutoPower(-70); // fiddle with this
#endif

  blink(LED, 3);
  delay(1000);
}

bool request()
{
  DEBUG_PRINT("tx REQUEST\n");

  char msg[1] = {REQUEST};
  bool ack = radio.sendWithRetry(GATEWAY_ID, msg, 1);
  radio.sleep();

  if (!ack) {
    DEBUG_PRINT("REQUEST failed\n");
    return false;
  }

  return true;
}

void updateHeater(char msg)
{
  bool powerToggle = ((msg & 0x1) == 0x1);
  int delta = (msg >> 1) & 0x3f;
  if ((msg & 0x80) != 0x0) {
    delta *= -1;
  }

  if (powerToggle) {
    power();
  }

  if (delta > 0) {
    tempUp(delta);
  } else if (delta < 0) {
    tempDown(abs(delta));
  }
}

void loop()
{
  static bool listen = false;
  static unsigned long requestTime = 0;

  if (listen) {
    if (radio.receiveDone()) {
      radio.sendACK();
      radio.sleep();

      DEBUG_PRINT("rx: %#x after %u ms\n", radio.DATA[0], millis() - requestTime);

      updateHeater(radio.DATA[0]);

      listen = false;
    } else {
      unsigned long elapsedTime = millis() - requestTime;
      if (elapsedTime > TIMEOUT)
      {
        listen = false;
        DEBUG_PRINT("rx timeout\n");
      }
    }
  } else {
    radio.sleep(); // very important, reduces idle current draw from 16 mA to 0.2~0.3 mA

#ifdef DEBUG
    Serial.flush();
#endif

    for (int i = 0; i < 8; ++i) {
      LowPower.powerDown(SLEEP_8S, ADC_OFF, BOD_OFF);
    }

    if (request()) {
      listen = true;
      requestTime = millis();
    }
  }
}

void initPins()
{
  pinMode(BTN_UP, OUTPUT);
  digitalWrite(BTN_UP, LOW);
  pinMode(BTN_DOWN, OUTPUT);
  digitalWrite(BTN_DOWN, LOW);
  pinMode(BTN_POW, OUTPUT);
  digitalWrite(BTN_POW, LOW);
}

void blink(byte pin, int duration)
{
  pinMode(pin, OUTPUT);
  digitalWrite(pin, HIGH);
  delay(duration);
  digitalWrite(pin, LOW);
}

void press(byte button, int duration)
{
  DEBUG_PRINT("press %d\n", button);
  pinMode(button, OUTPUT);
  pinMode(LED, OUTPUT);
  digitalWrite(button, HIGH);
  digitalWrite(LED, HIGH);
  delay(duration);
  digitalWrite(button, LOW);
  digitalWrite(LED, LOW);
  delay(200);
}

void power()
{
  DEBUG_PRINT("switch power\n");
  press(BTN_POW, 1500);
}

void changeTemp(byte button, int count)
{
  for (int i = 0; i < count + 1; i++) { // extra initial press to change temp
    press(button, 400);
  }
}

void tempUp(int count)
{
  DEBUG_PRINT("temp up %d\n", count);
  changeTemp(BTN_UP, count);
}

void tempDown(int count)
{
  DEBUG_PRINT("temp down %d\n", count);
  changeTemp(BTN_DOWN, count);
}
