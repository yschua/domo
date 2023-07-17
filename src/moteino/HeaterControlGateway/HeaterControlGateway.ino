#include <RFM69.h>
#include <RFM69_ATC.h>

// Device
//#define FREQUENCY   RF69_868MHZ
#define FREQUENCY   RF69_915MHZ
#define ENCRYPT_KEY "0123456789abcdef" // 16 bytes
#define IS_RFM69HW
#define ENABLE_ATC // Auto Transmission Control

// Pins
#define LED         9

// Network
#define NODE_ID     1    // Unique
#define NETWORK_ID  100  // Same
#define HEATER_ID   2
#define SERIAL_BAUD 115200

// Global
#ifdef ENABLE_ATC
  RFM69_ATC radio;
#else
  RFM69 radio;
#endif

const char REQ_STATUS = 0;
const char REQ_TOGGLE = 1;

const char RESP_NO_CHANGE = 0;
const char RESP_TOGGLE_CONFIRM = 1;

const char RADIO_NOP = 0;
const char RADIO_TOGGLE = 1;

void setup()
{
  Serial.begin(SERIAL_BAUD);
  while (!Serial)
  {
  }
  radio.initialize(FREQUENCY, NODE_ID, NETWORK_ID);

#ifdef IS_RFM69HW
  radio.setHighPower();
#endif

  radio.encrypt(ENCRYPT_KEY);
  blink(LED, 3);
}

void loop()
{
  static bool toggleRequested = false;
  static bool toggleConfirmed = false;

  if (Serial.available() > 0)
  {
    if (Serial.read() == REQ_TOGGLE)
    {
      toggleRequested = !toggleRequested;
    }

    if (toggleConfirmed)
    {
      toggleConfirmed = false;
      Serial.write(RESP_TOGGLE_CONFIRM);
    }
    else
    {
      Serial.write(RESP_NO_CHANGE);
    }
  }

  if (radio.receiveDone())
  {
    radio.sendACK();

    char response[1];
    response[0] = toggleRequested ? RADIO_TOGGLE : RADIO_NOP;
    bool ack = radio.sendWithRetry(HEATER_ID, response, 1);

    if (toggleRequested && ack)
    {
      toggleConfirmed = true;
      toggleRequested = false;
    }
  }
}

void blink(byte pin, int duration)
{
  pinMode(pin, OUTPUT);
  digitalWrite(pin, HIGH);
  delay(duration);
  digitalWrite(pin, LOW);
}
