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

// Message control
const char SUCCESS = 0x0;
const char FAILED = 0x1;

void setup()
{
  Serial.begin(SERIAL_BAUD);
  delay(10); // need?
  radio.initialize(FREQUENCY, NODE_ID, NETWORK_ID);

#ifdef IS_RFM69HW
  radio.setHighPower();
#endif

  radio.encrypt(ENCRYPT_KEY);

  blink(LED, 3);
}

void loop()
{
  if (Serial.available() > 0) {
    char msg[1] = {Serial.read()};
    bool ack = radio.sendWithRetry(HEATER_ID, msg, 1);
    Serial.write(ack ? SUCCESS : FAILED);
  }

  if (radio.receiveDone()) {
    radio.sendACK();
    Serial.write(radio.DATA[0]);
  }
}

void blink(byte pin, int duration)
{
  pinMode(pin, OUTPUT);
  digitalWrite(pin, HIGH);
  delay(duration);
  digitalWrite(pin, LOW);
}
