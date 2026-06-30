#include <BluetoothSerial.h>
#include <Preferences.h>

// ── Configurazione ────────────────────────────────────────────────────────────
#define DEVICE_NAME  "ESP32-Luce"
#define PIN_LUCE     26   // GPIO collegato al relè o LED — modifica se necessario
#define PIN_LED_BT   2    // LED onboard: lampeggia = in attesa, fisso = connesso

BluetoothSerial BT;
Preferences     prefs;

bool luceAccesa    = false;
bool btConnesso    = false;

// ── Callback eventi Bluetooth ─────────────────────────────────────────────────
void onBtEvent(esp_spp_cb_event_t event, esp_spp_cb_param_t* param)
{
    if (event == ESP_SPP_SRV_OPEN_EVT) {
        btConnesso = true;
        Serial.println("Client connesso");
        // invia subito lo stato corrente così l'app si aggiorna
        BT.printf("OK:%d\n", luceAccesa ? 1 : 0);
    }
    else if (event == ESP_SPP_CLOSE_EVT) {
        btConnesso = false;
        Serial.println("Client disconnesso");
    }
}

// ── Imposta stato luce ────────────────────────────────────────────────────────
void setLuce(bool on)
{
    luceAccesa = on;
    digitalWrite(PIN_LUCE, on ? HIGH : LOW);
    prefs.putBool("stato", on);          // salva in NVS (sopravvive al reboot)
    BT.printf("OK:%d\n", on ? 1 : 0);   // conferma all'app
    Serial.printf("Luce: %s\n", on ? "ON" : "OFF");
}

// ── Setup ─────────────────────────────────────────────────────────────────────
void setup()
{
    Serial.begin(115200);

    pinMode(PIN_LUCE,   OUTPUT);
    pinMode(PIN_LED_BT, OUTPUT);

    // ripristina ultimo stato salvato
    prefs.begin("luce", false);
    luceAccesa = prefs.getBool("stato", false);
    digitalWrite(PIN_LUCE, luceAccesa ? HIGH : LOW);
    Serial.printf("Stato ripristinato: %s\n", luceAccesa ? "ON" : "OFF");

    BT.register_callback(onBtEvent);
    if (!BT.begin(DEVICE_NAME)) {
        Serial.println("ERRORE: avvio Bluetooth fallito");
        while (true) { delay(500); }
    }

    Serial.printf("Bluetooth avviato come \"%s\"\n", DEVICE_NAME);
    Serial.println("In attesa di connessione...");
}

// ── Loop ──────────────────────────────────────────────────────────────────────
void loop()
{
    // LED di stato: lampeggio lento = in attesa, fisso = connesso
    if (btConnesso) {
        digitalWrite(PIN_LED_BT, HIGH);
    } else {
        digitalWrite(PIN_LED_BT, millis() % 1200 < 150 ? HIGH : LOW);
    }

    // leggi comandi in arrivo
    while (BT.available()) {
        char c = (char)BT.read();
        if      (c == '1') setLuce(true);
        else if (c == '0') setLuce(false);
        // ignora qualsiasi altro byte
    }
}
