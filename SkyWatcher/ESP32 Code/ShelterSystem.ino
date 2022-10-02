void setup() {
  Serial.begin(9600);
}

void loop() {
  Serial.println("Enter data:");
  while (Serial.available() == 0) {}     //wait for data available
  String teststr = Serial.readString();  //read until timeout
  teststr.trim();                        // remove any \r \n whitespace at the end of the String
  if (teststr == "open") 
  {
    Serial.println("Opened");
  }
  else if (teststr == "close") 
  {
    Serial.println("Closed");
  } 
  else 
  {
    Serial.println("Something else");
  }
}
