#include "FlipWare.h"

#define IR_USER_TIMEOUT_MS 10000
#define IR_EDGE_TIMEOUT_US 10000
#define IR_EDGE_REC_MAX 70

extern uint8_t IR_SENSOR_PIN;
extern uint8_t IR_LED_PIN;

uint8_t edges;
uint8_t timings[70];

void record_IR_command()
{
	unsigned long now = 0;
	unsigned long prev = 0;
	
	int duration;
	int i;
	int toggle = 1;
	int wait = 1;
	
	prev = millis();
	
	while(wait)	//wait for start bit or user timeout of 10s
	{	
		now = millis();
		duration = now - prev; //check how much time has passed
		
		if(duration >= IR_USER_TIMEOUT_MS) //user timeout
		{
			Serial.print("User timeout\n");
			wait = 0;		//leave the waiting loop
		}
		else if(!digitalRead(IR_SENSOR_PIN)) //start bit
		{
			Serial.print("Start condition\n");
			wait = 0;		//leave the waiting loop
		}
	}
	
	for(i=0; i<IR_EDGE_REC_MAX; i++)
	{
		prev = micros();
		wait = 1;
		while(wait)	//wait for next edge or edge timeout
		{
			now = micros();
			duration = now - prev;		//check how much time has passed
			if(duration >= IR_EDGE_TIMEOUT_US) 	//edge timeout -> IR command finished or disrupted
			{
				//Serial.print("Edge timeout\n");
				wait = 0;		//leave the waiting routine
				edges = i;
				i = IR_EDGE_REC_MAX;	//no more edges need to be measured
			}
			else if(digitalRead(IR_SENSOR_PIN) == toggle)	//check if edge apperared
			{
				wait = 0;		//leave the waiting routine
			}
		}	
		timings[i] = (now - prev) / 37;		//compress data
		//Serial.print(timings[i]);
		//Serial.print("\n");
		toggle = !toggle;		// toggle the expected edge event
	}
	Serial.print(edges);
	Serial.print("..........\n");
}

void play_IR_command()
{
	uint32_t edge_now = 0;
	uint32_t edge_prev = 0;	
	uint32_t duration = 0;
	uint8_t i;
	uint32_t state_time;
	boolean output_state = HIGH;
	
	for(i=0; i<edges; i++)
	{
		state_time = timings[i] * 37;	//decompress data
		edge_prev = micros();
		if(output_state == HIGH)
		{
			analogWrite(IR_LED_PIN, 128);	//activate burst (PWM with 50% duty cycle)
			do
			{
				edge_now = micros();
				duration = edge_now - edge_prev;
			}
			while(duration <= state_time);	//wait until saved time has passed
			analogWrite(IR_LED_PIN, 0);		//deactivate PWM
			output_state = LOW;				
		}
		else
		{
			digitalWrite(IR_LED_PIN,output_state);
			do
			{
				edge_now = micros();
				duration = edge_now - edge_prev;
			}
			while(duration <= state_time);	//wait until saved time has passed
			output_state = HIGH;
		}

	}
	digitalWrite(IR_LED_PIN,LOW);		//infrared LED must be turned of after this function
}