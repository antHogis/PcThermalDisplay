// Demo the quad alphanumeric display LED backpack kit
// scrolls through every character, then scrolls Serial
// input onto the display

#include "Adafruit_LEDBackpack.h"
#include <Adafruit_GFX.h>
#include <Wire.h>

Adafruit_AlphaNum4 alpha4_0 = Adafruit_AlphaNum4();
Adafruit_AlphaNum4 alpha4_1 = Adafruit_AlphaNum4();

String serial_buffer = "";

String default_string = "N/Ac";
String curr_string_0  = default_string;
String curr_string_1  = default_string;

void displayString(Adafruit_AlphaNum4* alpha4, String* str)
{
	for (uint8_t i = 0; i < 4; ++i)
	{
		alpha4->writeDigitAscii(i, str->charAt(i), i == 1 && str->charAt(i) != '/');
	}
	alpha4->writeDisplay();
}

void onSerialStringReceived(String* serial_string)
{
	String msg = String("got data ");
	msg.concat(*serial_string);
	Serial.println(msg);

	if (serial_string->length() == 4)
	{
		if (serial_string->charAt(0) == '0')
		{
			curr_string_0 = serial_string->substring(1);
			curr_string_0.concat("c");
		}
		else if (serial_string->charAt(0) == '1')
		{
			curr_string_1 = serial_string->substring(1);
			curr_string_1.concat("c");
		}
	}
}

void setup()
{
	Serial.begin(9600);

	alpha4_0.begin(0x70);
	alpha4_1.begin(0x72);

  alpha4_0.setBrightness(3);
  alpha4_1.setBrightness(5);

	alpha4_0.clear();
	alpha4_1.clear();

	alpha4_0.writeDisplay();
	alpha4_1.writeDisplay();
}

void loop()
{
	while (Serial.available())
	{
		char input_char = (char)Serial.read();

		if (input_char == '\n')
		{
			onSerialStringReceived(&serial_buffer);
			serial_buffer = "";
		}
		else
		{
			serial_buffer += input_char;
		}
	}

  // TODO nullify stale data

	displayString(&alpha4_0, &curr_string_0);
	displayString(&alpha4_1, &curr_string_1);

	delay(50);
}