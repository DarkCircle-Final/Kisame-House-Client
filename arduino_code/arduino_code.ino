/*
  라즈베리파이
*/

/*
  A0: gas,  A1: TDS, A2: PH
  
  D -> 2: DHT, 3: 수온, 4: 수위센서1, 5: 네오픽셀
  8,9,10,11 : 스텝 모터
  22, 23: 펌프1
========================== 밑에는 8.16 추가한것
  24, 25: 펌프2
  26: 수위센서2, 27: 수위센서3
  50: 릴레이1(히터), 51: 릴레이2(냉각)
  52: 릴레이3(산소), 53: 릴레이4(여과)

  수위1 은 새물 밑에 , 수위2는 현재 위에, 수위3은 현재 밑에
*/

// $는 해야 할 것.

// DHT22
#include "DHT.h"
#define DHTPIN 2    // 디지털핀 2임
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

// TDS
#define TdsSensorPin A1
#define VREF 5.0 
#define SCOUNT 30 
int analogBuffer[SCOUNT]; 
int analogBufferTemp[SCOUNT];
int analogBufferIndex = 0,copyIndex = 0;
float averageVoltage = 0,tdsValue = 0,temperature = 25;

// 수온
#include <OneWire.h>
#include <DallasTemperature.h>
#define ONE_WIRE_BUS 3
OneWire oneWire(ONE_WIRE_BUS);  // 준비
DallasTemperature sensors(&oneWire);
// ====== DS18B20 비동기 제어용 변수 ======
unsigned long lastWaterReq = 0;
bool waterPending = false;
const unsigned long waterPeriod = 2000;  // 2초마다 측정
const unsigned long waterConv = 190;     // 변환 대기시간 (10bit 해상도)
float wTemp = 0;

// 수위1
#define Water_level 4
int waterLevelValue = 0;   
// 수위2
#define Water_level2 38
int waterLevelValue2 = 0;   
// 수위3
#define Water_level3 39  
int waterLevelValue3 = 0;   

// 네오픽셀
#include <Adafruit_NeoPixel.h>
#define LED_PIN 5
#define LED_COUNT 13         // 실제로 점등할 개수
#define TOTAL_CONNECTED 60  // 실제로 물리적으로 연결된 개수
Adafruit_NeoPixel strip(TOTAL_CONNECTED, LED_PIN, NEO_GRBW + NEO_KHZ800);
#define BRIGHTNESS_H 250  // 밝기 강
#define BRIGHTNESS_M 130  // 밝기 중
#define BRIGHTNESS_L 60  // 밝기 약

// 스텝모터
#include <Stepper.h>
#define STEPS_PER_REV 2048
#define HALF_REV (STEPS_PER_REV / 2)
Stepper stepper(STEPS_PER_REV, 8, 10, 9, 11);
volatile int stepsToMove = 0;
unsigned long lastStepTime = 0;
unsigned long pauseStartTime = 0;
const int stepInterval = 3;  // ms당 1스텝(속도조절)  / 낮을수록 빠름
bool pauseFlag = false;
bool afterPause = false;
bool actionInProgress = false;  // 동작 중인지 여부
bool twoStageSpinActive = false; // 반바퀴-일시정지-반바퀴 모드
// 상태머신: 확실한 반바퀴-정지-반바퀴
enum StepState { STEP_IDLE, STEP_HALF1, STEP_PAUSE, STEP_HALF2 };
volatile StepState stepState = STEP_IDLE;

// 위치 추적 및 원점 복귀
long currentPosition = 0;           // 소프트웨어 기준 현재 위치
const long HOME_POSITION = 0;       // 시작 위치(전원 켤 때 0으로 가정)
volatile bool homingActive = false; // 원점 복귀 중 여부


// 펌프1
int AA = 22;
int AB = 23;
bool motorRunning = false;
bool pumpState = false;

// 펌프2
int BA = 28;
int BB = 29;
bool motorRunning2 = false;
bool pumpState2 = false;

// 릴레이
#define RELAY1_PIN 50
#define RELAY2_PIN 51
#define RELAY3_PIN 52
#define RELAY4_PIN 53

// PH
//필터 강도
//높을수록 필터효과가 강력하지만 1이 되면 안됨
#define alpha 0.9
#define PH7 3.99 // 기본 측정 값
float cal = 0; //보정상수
float old_ph = 0; //필터값

// on/off 로그용
// bool heater = false; // 히터
// bool fan = false; // 냉각팬
// bool O2 = false;  // 산소
// bool filtering = false; // 여과
bool step= false; // 먹이
bool LED_state = false; // LED


// 시간 추적용
unsigned long previousSensorTime = 0;
unsigned long sensorInterval = 2000; // 센서 주기: 2초

// 설정값
bool heater_start = false;       // 자동 제어 모드 on/off
bool manual_override1 = false;   // 수동 제어 여부
bool fan_start = false;
bool manual_override2 = false;
bool filtering_start = false;
bool manual_override3 = false;
bool pump_start = false;
bool manual_override4 = false;
bool pump_start2 = false;
bool manual_override4_2 = false;

bool feed_start = false;  // 먹이 가능하게
bool manual_override5 = false;
bool feed_state = false;  // 먹이 상태 저장용(먹이 돌아가기 위한)
bool feed_state2 = false;  // 먹이 상태 저장용(한바퀴 돌고 난 후 n누르고 m 누르고 n누르면 한바퀴 뒤로 돌아가는거 방지)

int heater_range = 24; // 히터 
int fan_range = 29;  // 쿨러
int tds_range1 = 150;   // tds
int tds_range2 = 250;   // tds
int tds_range3 = 0;   // tds
int tds_range4 = 0;   // tds
float ph_range1 = 6.8;    // ph
float ph_range2 = 7.2;    // ph
float ph_range3 = 0;    // ph
float ph_range4 = 0;    // ph

// 명령 매핑
typedef void (*CommandFunc)();

void cmda() { manual_override1 = true; digitalWrite(RELAY1_PIN, LOW); }
void cmdb() { manual_override1 = true; digitalWrite(RELAY1_PIN, HIGH); }
void cmdc() { manual_override2 = true; digitalWrite(RELAY2_PIN, LOW);  }
void cmdd() { manual_override2 = true; digitalWrite(RELAY2_PIN, HIGH);}
void cmde() { digitalWrite(RELAY3_PIN, LOW);} // 산소는 only 수동
void cmdf() { digitalWrite(RELAY3_PIN, HIGH);} // 산소는 only 수동
void cmdg() { manual_override3 = true; digitalWrite(RELAY4_PIN, LOW);}
void cmdh() { manual_override3 = true; digitalWrite(RELAY4_PIN, HIGH);}
void cmdi() { manual_override4 = true; digitalWrite(AA, HIGH); digitalWrite(AB, LOW);}
void cmdj() { manual_override4 = true; digitalWrite(AA, LOW);  digitalWrite(AB, LOW); }
void cmdk() { manual_override4_2 = true; digitalWrite(BA, HIGH); digitalWrite(BB, LOW);}
void cmdl() { manual_override4_2 = true; digitalWrite(BA, LOW);  digitalWrite(BB, LOW);}
void cmdm() {stepsToMove = HALF_REV; actionInProgress = true; stepState = STEP_HALF1; feed_state = true; } 
void cmdn() {
  stepState = STEP_IDLE;
  // 즉시 정지
  stepsToMove = 0;
  pauseFlag = false;
  afterPause = false;
  actionInProgress = false;
  // 원점 복귀 시작
  homingActive = true; } 

void cmdA() {neo_wh();} void cmdB() {neo_wm();} void cmdC() {neo_wl();} 
void cmdD() {neo_rh();} void cmdE() {neo_rm();} void cmdF() {neo_rl();} 
void cmdG() {neo_gh();} void cmdH() {neo_gm();} void cmdI() {neo_gl();} 
void cmdJ() {neo_bh();} void cmdK() {neo_bm();} void cmdL() {neo_bl();}
void cmdM() {neo_off();}

void cmdo() {heater_start = true; manual_override1 = false;} void cmdp() {heater_start = false; manual_override1 = true; }  // 
void cmdq() {fan_start = true; manual_override2 = false;} void cmdr() {fan_start = false; manual_override2 = true;}  //     
void cmds() {filtering_start = true; manual_override3 = false;} void cmdt() {filtering_start = false; manual_override3 = true;}  // 
void cmdu() {pump_start = true; manual_override4 = false;} void cmdv() {pump_start = false; manual_override4 = true;}  // 
void cmdy() {pump_start2 = true; manual_override4_2 = false;} void cmdz() {pump_start2 = false; manual_override4_2 = true;}  // 
void cmdw() {feed_start = true; feed_state= true; feed_state2=false;} void cmdx() {feed_start = false; feed_state = false;}  // 

void cmdN() {heater_start = true; manual_override1 = false; 
fan_start = true; manual_override2 = false; 
filtering_start = true; manual_override3 = false;
pump_start = true; manual_override4 = false;  pump_start2 = true; manual_override4_2 = false;
feed_start = true; feed_state= true; feed_state=false;}  // 입장 버튼 누르면 자동기능 on

void cmdO() {heater_range = 24;} void cmdP() {heater_range = 23;} void cmdQ() {heater_range = 22;}
void cmdR() {fan_range = 29;} void cmdS() {fan_range = 30;} void cmdT() {fan_range = 31;} 
void cmdU() {tds_range1 = 150; tds_range2 = 250;  ph_range1 = 6.8;  ph_range2 = 7.2;} void cmdV() {tds_range1 = 140; tds_range2 = 260;  ph_range1 = 6.6;   ph_range2 = 7.4;}
void cmdW() {tds_range3 = 130; tds_range4 = 270;  ph_range3 = 6.4;  ph_range4 = 7.6;} void cmdX() {tds_range3 = 120; tds_range4 = 280;   ph_range3 = 6.2;   ph_range4 = 7.8;}

struct Command {
  char key;
  CommandFunc func;
};

Command commands[] = {
  {'a', cmda},
  {'b', cmdb},
  {'c', cmdc},
  {'d', cmdd},
  {'e', cmde},
  {'f', cmdf},
  {'g', cmdg},
  {'h', cmdh},
  {'i', cmdi},
  {'j', cmdj},
  {'k', cmdk},
  {'l', cmdl},
  {'m', cmdm},
  {'n', cmdn},
  {'o', cmdo},
  {'p', cmdp},
  {'q', cmdq},
  {'r', cmdr},
  {'s', cmds},
  {'t', cmdt},
  {'u', cmdu},
  {'v', cmdv},
  {'w', cmdw},
  {'x', cmdx},
  {'y', cmdy},
  {'z', cmdz},
  {'A', cmdA},
  {'B', cmdB},
  {'C', cmdC},
  {'D', cmdD},
  {'E', cmdE},
  {'F', cmdF},
  {'G', cmdG},
  {'H', cmdH},
  {'I', cmdI},
  {'J', cmdJ},
  {'K', cmdK},
  {'L', cmdL},
  {'M', cmdM},
  {'N', cmdN},
  {'O', cmdO},
  {'P', cmdP},
  {'Q', cmdQ},
  {'R', cmdR},
  {'S', cmdS},
  {'T', cmdT},
  {'U', cmdU},
  {'V', cmdV},
  {'W', cmdW},
  {'X', cmdX},   
};

const int commandCount = sizeof(commands) / sizeof(commands[0]);

void setup() {
  Serial.begin(115200);
  dht.begin();
  pinMode(TdsSensorPin,INPUT);

  sensors.begin();
  sensors.setResolution(10);                  // 10-bit (187.5ms 변환시간)
  sensors.setWaitForConversion(false);        // 요청 후 기다리지 않음

  pinMode(Water_level,INPUT);
  pinMode(Water_level2,INPUT);
  pinMode(Water_level3,INPUT);
  digitalWrite(BA, LOW);
  digitalWrite(BB, LOW);
  digitalWrite(AA, LOW);
  digitalWrite(AB, LOW);
  pinMode(AA, OUTPUT);   // 펌프 핀 초기화
  pinMode(AB, OUTPUT);   // 펌프 핀 초기화
  pinMode(BA, OUTPUT);   // 펌프 핀 초기화
  pinMode(BB, OUTPUT);   // 펌프 핀 초기화

  stepper.setSpeed(12); // 일단 필요 없음, 수동 제어할 거니까
  
  strip.begin();   //  Neopixel 제어를 시작
  // strip.setBrightness(BRIGHTNESS);  // 네오픽셀 밝기 설정
  strip.show();   //  Neopixel 동작 초기화
  stepper.setSpeed(8); // 일단 필요 없음, 수동 제어할 거니까

  // 릴레이
  pinMode(RELAY1_PIN, OUTPUT);
  pinMode(RELAY2_PIN, OUTPUT);
  pinMode(RELAY3_PIN, OUTPUT);
  pinMode(RELAY4_PIN, OUTPUT);
  // 초기 OFF (LOW 트리거인 경우 HIGH로 설정)
  digitalWrite(RELAY1_PIN, HIGH);
  digitalWrite(RELAY2_PIN, HIGH);
  digitalWrite(RELAY3_PIN, HIGH);      
  digitalWrite(RELAY4_PIN, HIGH);

  // PH
  cal = (7-7) / (7- PH7); //보정상수 계산
  old_ph = read_ph(); //필터에 초기값 입력
}

void loop() {
  unsigned long currentMillis = millis();
  unsigned long currentMillis2 = millis();

  unsigned long now = millis();

  // 센싱 2
  if (currentMillis - previousSensorTime >= sensorInterval) {
    previousSensorTime = currentMillis;

    Serial.print("GAS:");
    Serial.println(analogRead(A0));
 
    waterLevelValue = digitalRead(Water_level);
    waterLevelValue2 = digitalRead(Water_level2);
    waterLevelValue3 = digitalRead(Water_level3);
    // 수위 출력인데 UI 표현 안해서 일단 블락
    // Serial.print("WATER_LEVEL1:");
    // Serial.println(waterLevelValue);
    // Serial.print("WATER_LEVEL2:");
    // Serial.println(waterLevelValue2);
    // Serial.print("WATER_LEVEL3:");
    // Serial.println(waterLevelValue3);

    dht22();
    tds();
    water_temp();
    PH_sensor();

    // 릴레이 on/off 출력
    if (digitalRead(RELAY1_PIN) == LOW) Serial.println("heater_ON");
    else Serial.println("heater_OFF");
    if (digitalRead(RELAY2_PIN) == LOW) Serial.println("fan_ON");
    else Serial.println("fan_OFF");
    if (digitalRead(RELAY3_PIN) == LOW) Serial.println("O2_ON");
    else Serial.println("O2_OFF");
    if (digitalRead(RELAY4_PIN) == LOW) Serial.println("filtering_ON");
    else Serial.println("filtering_OFF");

    // 펌프 상태 on/off 출력
    if (digitalRead(AA) == HIGH) Serial.println("PUMP1_ON");
    else Serial.println("PUMP1_OFF");
    if (digitalRead(BA) == HIGH) Serial.println("PUMP2_ON");
    else Serial.println("PUMP2_OFF");

    // 스텝 모터(먹이) on/off 출력
    if (actionInProgress) Serial.println("Feed_ON");
    else Serial.println("Feed_OFF");

    // LED on/off 출력 => 강중약 상관없이 on/off만 있음
    if (LED_state) Serial.println("LED_ON");
    else Serial.println("LED_OFF");

    // // 명령 먹는지 확인용 / 나중에 뺌 (디버깅용)
    // Serial.println(heater_range);
    // Serial.println(fan_range);
    // Serial.println(tds_range1);
    // Serial.println(tds_range2);
    // Serial.println(tds_range3);  
    // Serial.println(tds_range4);
    // Serial.println(ph_range1);
    // Serial.println(ph_range2);
    // Serial.println(ph_range3);
    // Serial.println(ph_range4);
    // Serial.println(heater_start);
    // Serial.println(fan_start);
    // Serial.println(filtering_start);
    // Serial.println(pump_start);
    // Serial.println(feed_start);    
    // Serial.println(feed_state);   
    // Serial.println(feed_state2);   
  }

  // 시리얼 통신
  if (Serial.available()) {
    char cmd = Serial.read();
    bool found = false;

    for (int i = 0; i < commandCount; i++) {
      if (commands[i].key == cmd) {
        commands[i].func();
        found = true;
        break;
      }
    }

    if (!found) {   // 명령 입력 안받으면 => 나중에 디버깅 용도
      // Serial.print("Unknown command: ");
      // Serial.println(cmd);
    }
  }
  
// 자동 기능
  if (heater_start && !manual_override1) {
    // 자동 제어 동작
    if (heater_range >= wTemp) {
      digitalWrite(RELAY1_PIN, LOW);
    } else {
      digitalWrite(RELAY1_PIN, HIGH);
    }
  }


if(fan_start && !manual_override2)
{
  if(fan_range<=wTemp)
  {
    digitalWrite(RELAY2_PIN, LOW);
  }
  else
  {
    digitalWrite(RELAY2_PIN, HIGH);
  }
}

if(filtering_start && !manual_override3)
{       
  if(( (tds_range3<tdsValue<= tds_range1)  || (tds_range4 >tdsValue >=tds_range2)) && ( (ph_range1 >= old_ph > ph_range3) || (ph_range2 <= old_ph < ph_range4)))
  {
    digitalWrite(RELAY4_PIN, LOW);
  }
  else
  {
    digitalWrite(RELAY4_PIN, HIGH);
  }
}


if((pump_start && !manual_override4) && (pump_start2 && !manual_override4_2))
{
  if((tds_range1>=tdsValue || tds_range2<=tdsValue) && (ph_range1 >= old_ph || ph_range2 <= old_ph) )
  {      
        // 새 물
        if (waterLevelValue == 1 && Water_level2 == 0){
          digitalWrite(AA, HIGH); // 정방향 회전이고 반대로 하면 역방향 호스 -> 펌프 -> 물 순서로 될듯.
          digitalWrite(AB, LOW);}
        else{
          digitalWrite(AA, LOW);
          digitalWrite(AB, LOW); 
        }
        // 현재 물
        if (waterLevelValue3 == 1){
          digitalWrite(BA, HIGH); 
          digitalWrite(BB, LOW);}
        else{
          digitalWrite(BA, LOW);
          digitalWrite(BB, LOW); 
        }
  }
}

// 스텝모터 동작 처리 (상태머신)
  if(feed_start && feed_state){
  if (actionInProgress) {
    feed_state2 = true;
    if (currentMillis - lastStepTime >= stepInterval) {
      switch (stepState) {
        case STEP_HALF1:
          if (stepsToMove > 0) {
            stepper.step(1);
            stepsToMove--;
            currentPosition++;
            lastStepTime = currentMillis;
            if (stepsToMove == 0) {
              pauseStartTime = millis();
              stepState = STEP_PAUSE;
              // Serial.println("Reached HALF1 -> PAUSE");
            }
          }
          break;
        case STEP_PAUSE:
          if (millis() - pauseStartTime >= 500) {
            stepsToMove = HALF_REV;
            stepState = STEP_HALF2;
            // Serial.println("PAUSE over -> HALF2");
          }
          break;
        case STEP_HALF2:
          if (stepsToMove > 0) {
            stepper.step(1);
            stepsToMove--;
            currentPosition++;
            lastStepTime = currentMillis;
            if (stepsToMove == 0) {
              stepState = STEP_IDLE;
              actionInProgress = false;
              feed_state2 = false;
              // Serial.println("HALF2 done -> IDLE");
            }
          }
          break;
        case STEP_IDLE:
        default:
          actionInProgress = false;
          break;
      }
    }
  }
  }
  if (feed_start == 0)
  {
    feed_state=false;
  }
  if (feed_state2 == 0)
  {
    currentPosition = 0;
  }
  // ===== 원점 복귀 처리 =====
  if(feed_state2){
    if (homingActive) {
      if (currentMillis - lastStepTime >= stepInterval) {
        if (currentPosition > HOME_POSITION) {
          stepper.step(-1);
          currentPosition--;
          lastStepTime = currentMillis;
        } else if (currentPosition < HOME_POSITION) {
          stepper.step(1);
          currentPosition++;
          lastStepTime = currentMillis;
        } else {
          homingActive = false; // 원점 도달
          feed_state2 = false;
        }
      }
    }  
  }

}
void dht22(){
    float h = dht.readHumidity();
    float t = dht.readTemperature();

  if (!isnan(t) && !isnan(h)) {
    Serial.print("HUM:");
    Serial.println(h);
    Serial.print("TEMP:");
    Serial.println(t);
    }
}

void tds(){
  static unsigned long analogSampleTimepoint = millis();
  if(millis()-analogSampleTimepoint > 40U) 
  {
  analogSampleTimepoint = millis();
  analogBuffer[analogBufferIndex] = analogRead(TdsSensorPin); 
  analogBufferIndex++;
  if(analogBufferIndex == SCOUNT)
  analogBufferIndex = 0;
  }
  static unsigned long printTimepoint = millis();
  if(millis()-printTimepoint > 800U)
  {
  printTimepoint = millis();
  for(copyIndex=0;copyIndex<SCOUNT;copyIndex++)
  analogBufferTemp[copyIndex]= analogBuffer[copyIndex];
  averageVoltage = getMedianNum(analogBufferTemp,SCOUNT) * (float)VREF/ 1024.0; 
  float compensationCoefficient=1.0+0.02*(temperature-25.0);
  float compensationVolatge=averageVoltage/compensationCoefficient; 
  tdsValue=(133.42*compensationVolatge*compensationVolatge*compensationVolatge - 255.86*compensationVolatge*compensationVolatge + 857.39*compensationVolatge)*0.5;
    Serial.print("TDS:");
    Serial.println(tdsValue, 0);
}
}

int getMedianNum(int bArray[], int iFilterLen)  // tds 함수
{
  int bTab[iFilterLen];
  for (byte i = 0; i<iFilterLen; i++)
  bTab[i] = bArray[i];
  int i, j, bTemp;
  for (j = 0; j < iFilterLen - 1; j++)
  {
  for (i = 0; i < iFilterLen - j - 1; i++)
  {
  if (bTab[i] > bTab[i + 1])
  {
  bTemp = bTab[i];
  bTab[i] = bTab[i + 1];
  bTab[i + 1] = bTemp;
  }
  }
  }
  if ((iFilterLen & 1) > 0)
  bTemp = bTab[(iFilterLen - 1) / 2];
  else
  bTemp = (bTab[iFilterLen / 2] + bTab[iFilterLen / 2 - 1]) / 2;
  return bTemp;
}

void water_temp()
{
  sensors.requestTemperatures();
  wTemp = sensors.getTempCByIndex(0);
  Serial.print("WATER_TEMP:");
  Serial.println(wTemp);
  // -127 나오면 하드웨어 문제(연결 등등) 
}

void neo_wh() // 화이트 - 강
{
  strip.setBrightness(BRIGHTNESS_H);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 0, 255);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_wm() // 화이트 - 중
{
  strip.setBrightness(BRIGHTNESS_M);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 0, 255);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_wl() // 화이트 - 약
{
  strip.setBrightness(BRIGHTNESS_L);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 0, 255);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_rh() // 레드 - 강
{
  strip.setBrightness(BRIGHTNESS_H);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 250, 0, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_rm() // 레드 - 중
{
  strip.setBrightness(BRIGHTNESS_M);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 250, 0, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_rl() // 레드 - 약
{
  strip.setBrightness(BRIGHTNESS_L);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 250, 0, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_gh() // 그린 - 강
{
  strip.setBrightness(BRIGHTNESS_H);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 250, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_gm() // 그린 - 중
{
  strip.setBrightness(BRIGHTNESS_M);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 250, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_gl() // 그린 - 약
{
  strip.setBrightness(BRIGHTNESS_L);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 250, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_bh() // 블루 - 강
{
  strip.setBrightness(BRIGHTNESS_H);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 250, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_bm() // 블루 - 중
{
  strip.setBrightness(BRIGHTNESS_M);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 250, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_bl() // 블루 - 약
{
  strip.setBrightness(BRIGHTNESS_L);  // 네오픽셀 밝기 설정
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 250, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = true;
  strip.show();
}

void neo_off()
{
  for (int i = 0; i < TOTAL_CONNECTED; i++) {
  if (i < LED_COUNT) {
    strip.setPixelColor(i, 0, 0, 0, 0);  // 하얀색(W 채널)
    } else {
      strip.setPixelColor(i, 0, 0, 0, 0);    // 꺼짐
    }
  }
  LED_state = false;
  strip.show();
}

void PH_sensor()
{
  float now_ph = read_ph(); //현재 ph값 측정
  //현재값 10%, 과거값 90% 비율로 필터적용
  old_ph = now_ph*(1-alpha) + old_ph*alpha;
  Serial.print("PH:");
  Serial.println(old_ph);
}

float read_ph()
{
  float Po = analogRead(A0) * 5.0 / 1023; //전압
  float phValue = 7 - (2.5 - Po) * cal; //전압으로 ph농도 계산
  return phValue; //반환
}

void water_pump1()
{  
  if (!pumpState) {
    digitalWrite(AA, HIGH); // 정방향 회전이고 반대로 하면 역방향 호스 -> 펌프 -> 물 순서로 될듯.
    digitalWrite(AB, LOW);
    }
  else {
    if (pumpState) {
      digitalWrite(AA, LOW);
      digitalWrite(AB, LOW);
    }
  }
}

void water_pump2()
{  
  if (!pumpState2) {
    digitalWrite(BA, HIGH); // 정방향 회전이고 반대로 하면 역방향 호스 -> 펌프 -> 물 순서로 될듯.
    digitalWrite(BB, LOW);
    }
  else {
    if (pumpState2) {
      digitalWrite(BA, LOW);
      digitalWrite(BB, LOW);
    }
  }
}