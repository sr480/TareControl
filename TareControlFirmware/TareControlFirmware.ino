String N_zap = ""; //хранимый номер запроса
String N_zap_temp = "";
String Command = "";
String Answer = "";
String Argument = "";

char newRX = ' ';

int i = 0;
int dir = 22;
int _step = 26;
int EnServo = 30;
int Etx = 41;
//int Erx = 38;
int step_counter = 0;
int destination = 0;
int N_sens = 0;
int DATA_1 = 0;
int DATA_2 = 0;
int TEMPERAT = 0;
int led = 13;
int global_step_counter = 0;
int global_step_counter_temp = 0;
int DATA;
int max_count;

boolean stepup = false;
boolean N_zapr = true;
boolean Argum = false;


int zn_sch = 0; //указатель для значений и счетчиков
int t = 0; //указатель для приёма
int k = 0; //указатель для передаваемых запросов

int REZULT[14]; // буфер с неусреднёнными значениями
long TEMP_REZULT[14];
int COUNTERS[14]; // буфер с счетчиками для усреднения
int TEMPERATURE[7]; // буфер с температурами
int MGN_REZULT[14]; //буфер с мгновенными результатами
int OK_REZULT[14]; //буфер с усредненными результатами
int ADRESS[7];
long DATA_BUF[28];

unsigned int error_counter = 0;
unsigned int error_counter_max = 0;

int KOM[28] = {0x91,0x92,0xA1,0xA2,0xB1,0xB2,0xC1,0xC2,0xD1,0xD2,0xE1,0xE2,0xF1,0xF2,0x97,0xA7,0xB7,0xC7,0xD7,0xE7,0xF7,0x94,0xA4,0xB4,0xC4,0xD4,0xE4,0xF4};
//int KOM[4] = {0xB7,0xB1,0xB2};
int info[28];

boolean get_adress = false;
boolean get_temperature = false;
boolean get_rezult = true;
boolean low_step = false;
boolean temperature_time = false;
boolean error_inf = false;
boolean Status = false;
boolean temperature = false;
boolean adres = false;
boolean stop_t = false;

unsigned long time_1;
unsigned long time_2;
unsigned long last_step;
unsigned long time_receive;
unsigned long time_counter;
unsigned long time_wait;
unsigned long timer_temperature;


int counter_2 = 0;
int tempCounter;
int counter_rx = 0;

void setup()
{
 //назначаем пины для управления шаговым двигателем
 //и выводим на них "1"
  pinMode(dir, OUTPUT); 
  pinMode(_step, OUTPUT);
  pinMode(EnServo, OUTPUT);
  digitalWrite(dir, HIGH);
  digitalWrite(_step, HIGH);
  
  digitalWrite(EnServo, LOW);
  
 //настраиваем 1й порт ввода/вывода (USB)
  Serial.begin(115200);
  //настраиваем 2й порт ввода/вывода (RS-485)
  Serial1.begin(19200);
  pinMode(Etx, OUTPUT);//разрешаем передачу драйверу RS-485
  digitalWrite(Etx, HIGH);
//  pinMode(Erx, OUTPUT);//запрещаем прием драйверу RS-485
//  digitalWrite(Etx, HIGH);
  
  boolean get_adress = true;
  
  UCSR1A = 0x60;
  UBRR1L = 51;
  
  pinMode(led, OUTPUT);
  time_counter = millis();
  error_counter = 0;

  max_count = 128;
  time_wait = 20;
  get_adress = false;
  get_temperature = false;
  get_rezult = true;
  stop_t = false;

  counter_rx = 0;
  tempCounter = 100;
}

void loop()
{
    if (i != step_counter)
    {
       if ((millis() - time_2) >= 50)
       {
           if (low_step)
           {
               digitalWrite(_step, LOW);
               low_step = false;
               i++;
           }
           else
           {
               digitalWrite(_step, HIGH);
               low_step = true;
           }
           time_2 = millis();
       }
    }
    
    // проверка сколько прошло времени и послыка/приём байт, если необходимо
    if (millis() - time_counter >= time_wait)
    {
         while (Serial1.available())
         {
          byte temp_byte = (byte)Serial1.read();

          //if  (temp_byte != KOM[k])
          if  (counter_rx == 2)
            {
            error_counter = 0;
            error_inf = false;
            
            //byte byte_rx_1 = (byte)Serial1.read();
            byte byte_rx_1 = temp_byte;
            byte byte_rx_2 = (byte)Serial1.read();
            
            bitClear(byte_rx_1,7);
            DATA = byte_rx_1;
            DATA = DATA << 7;
            DATA = DATA + byte_rx_2;

            /*
            Serial.print("counter_rx: ");
            Serial.print(counter_rx);
            Serial.print(" DATA ");
            Serial.println(DATA);
            */          
            }
          if  (temp_byte = KOM[k] && counter_rx < 2)
          {
            counter_rx = counter_rx + 1;
            /*
            Serial.print("counter_rx: ");
            Serial.print(counter_rx);
            Serial.print(" byte ");
            Serial.println(temp_byte);
            */
          }
         }

         counter_rx = 0;

          if (DATA == 0xFFFF)
          {
            error_inf = true;
            error_counter++;
            
            if (error_counter == 2)
            {
                error_counter = 0;                
                error_inf = false;
            }
          }

 /*   
        //вытаскиваем принятые байты и обрабатываем их
        if (Serial1.available() != 2)
        {
            error_inf = true;
            error_counter++;
            
            if (error_counter == 2)
            {
                DATA = 0xFFFF;
                error_counter = 0;                
                error_inf = false;
            }
           
        }

        else
        {
            error_counter = 0;
            error_inf = false;
            
            byte byte_rx_1 = (byte)Serial1.read();
            byte byte_rx_2 = (byte)Serial1.read();
            
            bitClear(byte_rx_1,7);
            DATA = byte_rx_1;
            DATA = DATA << 7;
            DATA = DATA + byte_rx_2;
        }
*/

        if (error_inf == false)
        {
          // проверяем - температуру мы собираем или с датчиков
          if (get_temperature)
          {
              if (DATA != 0xFFFF)
              {
                  if (bitRead(DATA, 12) != 1)
                  {
                      //DATA = ((DATA << 5) >> 6);// положительная температура
                  }
                  else
                  {
                      bitClear(DATA, 12);
                      bitClear(DATA, 13);
                      bitClear(DATA, 14);
                      bitClear(DATA, 15);
                      DATA = 0 - DATA;// отрицательная температура
                  }
              }
              TEMPERATURE[t] = DATA;
              t++;
              k++;
              if (t == 7)
              {
                    get_temperature = false;
                    get_rezult = true;
                    t = 0;
                    /*
                    for (int z = 0; z != 14; z++)
                    {
                      Serial.print(z);
                      Serial.print(": ");
                      Serial.println(REZULT[z]);
                    }
                    for (int z = 0; z != 7; z++)
                    {
                      Serial.print(z);
                      Serial.print(": ");
                      Serial.println(ADRESS[z]);
                    }
                    for (int z = 0; z != 7; z++)
                    {
                      Serial.print(z);
                      Serial.print(": ");
                      Serial.println(TEMPERATURE[z]);
                    }
                    */
              }
          }
          else if (get_rezult)
          {
              //MGN_REZULT[t] = DATA;

              if (DATA == 0xFFFF)
              {
                   REZULT[t] = 0xFFFF;
                   COUNTERS[t] = 0;
                   TEMP_REZULT[t] = 0;
                   k++;
                   t++;

              }
              else
              {
                  if (COUNTERS[t] == 63)
                  {
                      
                    
                      TEMP_REZULT[t] = TEMP_REZULT[t] + DATA;
                      REZULT[t] = TEMP_REZULT[t] / 64;
                      TEMP_REZULT[t] = 0;
                      COUNTERS[t] = 0;
                      k++;
                      t++;
                      if (t == 14)
                      {
                          get_adress = true;
                          get_rezult = false;
                          t = 0;
                      }
                  }
                  else
                  {
                    /*
                      Serial.print(COUNTERS[t]);
                      Serial.print(": ");
                      Serial.println(DATA);
                      */
                      TEMP_REZULT[t] = TEMP_REZULT[t] + DATA;
                      COUNTERS[t] = COUNTERS[t] + 1;
                  }
              }

              if (t == 14)
              {
                    get_adress = true;
                    get_rezult = false;
                    t = 0;
              }
          }
          else if (get_adress)
          {
            
              if (DATA != 0x0000)
              {
                  ADRESS[t] = DATA;
              }
              else
              {
                  ADRESS[t] = -1;
              }
              t++;
              k++;
              if (t == 7)
              {
                    get_adress = false;
                    get_temperature = true;
                    t = 0;
                    
                    if (stop_t == true)
                    {
                       get_temperature = false;
                       get_rezult = true;
                       k  = 0;
                    /*   
                    for (int z = 0; z != 14; z++)
                    {
                      Serial.print(z);
                      Serial.print(": ");
                      Serial.println(REZULT[z]);
                    }
                    for (int z = 0; z != 7; z++)
                    {
                      Serial.print(z);
                      Serial.print(": ");
                      Serial.println(ADRESS[z]);
                    }
                    for (int z = 0; z != 7; z++)
                    {
                      Serial.print(z);
                      Serial.print(": ");
                      Serial.println(TEMPERATURE[z]);
                    }
                    */
                    }
              }
          }
        }

        if (k == 13)
        {
            max_count = 1;
        }
        if (k == 21)
        {
           max_count = 1;
           //time_wait = 2500;
           time_wait = 1500;
           //time_wait = 500;
        }
        if (k  == 0)
        {
            max_count = 128;
            time_wait = 50;
        }
        if (k  == 28)
        {
             k = 0;
        }
        digitalWrite(Etx, HIGH);
        
        delay(1);
        while (Serial1.available())
         {
           byte temp_byte = (byte)Serial1.read();
         }

        Serial1.write(byte(KOM[k]));
        Serial1.write(byte(KOM[k]));
        Serial1.flush();
        
        //    Serial1.write(byte(0xB1));
        //    Serial1.write(byte(0xB1));
        //     Serial1.write(byte(0x97));
        //    Serial1.write(byte(0x97));       
        
        digitalWrite(Etx, LOW);
/*
         while (Serial1.available())
         {
           byte temp_byte = (byte)Serial1.read();
         }
*/
        DATA = 0xFFFF;
        time_counter = millis();
    }
}


void serialEvent()
{
  while (Serial.available())
  {
      newRX = (char)Serial.read();
      //    Serial.print(newRX);
    
      if (N_zapr == true)
      //принимаем номер запроса
      {
         if (newRX == ':')
         {
            N_zap = N_zap_temp;
            N_zap_temp = "";
            N_zapr = false;
         }
         else
         { 
           if (newRX != '<')
           {
              N_zap_temp += newRX;
           }
         }
      }
      else if (Argum != false)
      {
        //принимаем аргумент
        if (newRX == '>')
        {
            if (Status == true)
            {
                if (Argument == "STEP")
                {
                      if (stepup)
                      {
                        global_step_counter_temp = global_step_counter + i;
                      }
                      else
                      {
                        global_step_counter_temp = global_step_counter - i;
                      }
                      /*
                      if (stepup)
                     {
                         global_step_counter += step_counter;
                     }
                     else
                     {
                         global_step_counter -= step_counter;
                     }
                     */
                     //Serial.print('<' + N_zap + ":" + global_step_counter_temp + ">"); 
                     Serial.print('<');
                     Serial.print(N_zap);
                     Serial.print(':');
                     Serial.print(global_step_counter_temp);
                     Serial.print('>');
                }
                else
                {
                     int sdvig = Argument.toInt();
                     sdvig = sdvig + sdvig;
                     /*
                     Serial.println(sdvig);
                     Serial.println(REZULT[sdvig]);
                     Serial.println(REZULT[sdvig]);
                     */
                     if (sdvig <=12)
                     {
                       //digitalWrite(led, LOW);
                         //Serial.print('<' + N_zap + ":" + REZULT[sdvig] + " " + REZULT[sdvig + 1] + ">");
                         Serial.print('<');
                         Serial.print(N_zap);
                         Serial.print(':');
                         Serial.print(REZULT[sdvig]);
                         Serial.print(' ');
                         Serial.print(REZULT[sdvig+1]);
                         Serial.print('>');
                     }
                }
                Status = false;
            }
            else if(temperature == true)
            {
                int sdvig = Argument.toInt();
                if (sdvig <= 6)
                {
                    //Serial.print('<' + N_zap + ":" + TEMPERATURE[sdvig] + ">"); 
                    Serial.print('<');
                    Serial.print(N_zap);
                    Serial.print(':');
                    Serial.print(TEMPERATURE[sdvig]);
                    Serial.print('>');
                }
                temperature = false;
            }
            else if(adres == true)
            {
                int sdvig = Argument.toInt();
                if (sdvig <= 6)
                {
                    //Serial.print('<' + N_zap + ":" + ADRESS[sdvig] + ">"); 
                    Serial.print('<');
                    Serial.print(N_zap);
                    Serial.print(':');
                    Serial.print(ADRESS[sdvig]);
                    Serial.print('>');
                    /*
                    Serial.print('\n');
                    Serial.print('\n');
                    Serial.print(ADRESS[0]);
                    Serial.print('\n');
                    Serial.print(ADRESS[1]);
                    Serial.print('\n');
                    Serial.print(ADRESS[2]);
                    Serial.print('\n');
                    Serial.print(ADRESS[3]);
                    Serial.print('\n');
                    Serial.print(ADRESS[4]);
                    Serial.print('\n');
                    Serial.print(ADRESS[5]);
                    Serial.print('\n');
                    Serial.print(ADRESS[6]);
                    Serial.print('\n');
                    */
                }
                adres = false;
            }
            else
            {
                digitalWrite(EnServo, HIGH);
                
                step_counter = Argument.toInt();
                //Serial.print('<' + N_zap + ":OK" + '>');
                Serial.print('<');
                Serial.print(N_zap);
                Serial.print(":OK>");
            }
            N_zapr = true;
            Argum = false;
            Argument = "";
            Command = "";
        }
        else
        {
            Argument += newRX;
        }
      }      
      else
      {  
        //принимаем команду
        if (newRX == ' ' || newRX == '>')
        {
            //разбираем команду
            if (Command == "START_T")
            {
                //Serial.print('<' + N_zap + ":OK" + '>');
              Serial.print('<');
              Serial.print(N_zap);
              Serial.print(":OK>");
                
                N_zapr = true;
                Argum = false;
                Command = "";
                stop_t = false;
            }
            if (Command == "STOP_T")
            {
                //Serial.print('<' + N_zap + ":OK" + '>');
              Serial.print('<');
              Serial.print(N_zap);
              Serial.print(":OK>");
                
                N_zapr = true;
                Argum = false;
                Command = "";
                stop_t = true;
            }
            if (Command == "CONNECT")
            {
              //  Serial.print('<' + N_zap + ":OK" + '>');
              Serial.print('<');
              Serial.print(N_zap);
              Serial.print(":OK>");
              
                N_zapr = true;
                Argum = false;
                Command = "";
            }
            if (Command == "SETHOME")
            {
              //Serial.print('<' + N_zap + ":OK" + '>');
              Serial.print('<');
              Serial.print(N_zap);
              Serial.print(":OK>");
              step_counter = 0;
              i = 0;
              global_step_counter = 0;
              N_zapr = true;
              Argum = false;
              Command = "";
            }
            if (Command == "STEPUP")
            {
              stepup = true;
              Argum = true;
              digitalWrite(dir, HIGH);
              Command = "";
            }
            if (Command == "STEPDN")
            {
              stepup = false;
              Argum = true;
              digitalWrite(dir, LOW);
              Command = "";
            }
            if (Command == "TEMPERATURE")
            {
                temperature = true;
                Argum = true;
                Command = "";
            }
            if (Command == "ADRES")
            {
                adres = true;
                Argum = true;
                Command = "";
            }
            if (Command == "STATUS")
            {
                if (i == step_counter)
                {
                  digitalWrite(EnServo, LOW);  
                  if (stepup)
                  {
                         global_step_counter += step_counter;
                  }
                  else
                  {
                         global_step_counter -= step_counter;
                  }
                  step_counter = 0;
                  i = 0;
                }
                Argum = true;
                Status = true;
                Command = "";
            }
        }
        else
        { 
           Command += newRX;
        }
      }
   }
}


