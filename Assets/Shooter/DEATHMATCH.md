# Deathmatch arena

Sahne: `Assets/Scenes/DeathmatchArena.unity`. Unity menüsü: **Shooter > Deathmatch arenasını aç**.

Play'e basın ve oyun alanına tıklayın. Tek oyuncu ve beş bot olmak üzere altı kişi, herkesin tek olduğu bir maçta yarışır. İlk 10 öldürmeye ulaşan kazanır. Botlar birbirlerine de saldırır ve kazanabilir.

| Kontrol | İşlev |
| --- | --- |
| WASD / fare | Hareket / nişan |
| Sol tık (basılı tutulabilir) | Ateş |
| R | Şarjör değiştir |
| Tab (basılı tut) | Altı kişinin öldürme ve ölme sayıları |
| Esc | Oyunu duraklat / fareyi serbest bırak |
| Enter (maç bitince) | Yeni harita ve yeni maç |

100 can. Gövde hasarı 34 (üç isabette öldürme), kafa hasarı 100. Headshot; büyük turuncu isabet işareti, HEADSHOT yazısı, farklı ses, daha fazla parçacık ve daha güçlü düşme tepkisi verir. Bacak isabetleri gövde hasarı sayılır. Siperler her iki tarafın atışını keser.

Ölen kişi üç saniye sonra yeniden doğar. Canı ve mühimmatı yenilenir; 1,25 saniye doğma koruması alır. Doğma noktası, yaşayan rakiplere uzaklığı ve görünürlüğü dikkate alınarak seçilir. Ölümde devam eden şarjör değiştirme iptal edilir. Maç sonunda tüm hasar ve ateş durur. Arenada poligondaki T mühimmat hilesi kapalıdır.

Harita 30 × 30 metre, 15 × 15 hücredir. Her Play girişinde ve yeni maçta yeni seed kullanılır. Ortadaki iki ana geçiş, çevre koridoru ve sekiz doğma alanı açık kalır. Değişken uzunluk, yerleşim ve yükseklikte siper adaları üretilir. Bir ada ancak tüm boş hücreler bağlantılı kalıyorsa kabul edilir. Botlar bu ızgara üzerinde en kısa yolları bulur, görüş alanındaki en yakın rakibi seçer ve basit, düşük isabetli seri atışlar yapar; sekiz atışta şarjör değiştirirler.

**Ayarlama:** `DEATHMATCH / First to 10 > Arena Game` altında öldürme hedefi ve yeniden doğma süresi; `Player > Range Weapon` altında hasar ve silah ayarları; botların `Arena Bot` bileşeninde hareket ve atış hızı bulunur. Renkli gövdeler ve beyaz kafalar botları ayırt etmeye yardımcı olur.

**Doğrulama:** `Shooter > Deathmatch mekaniklerini doğrula`, 32 seed için bağlantı ve doğma kurallarını; gerçek kafa/gövde ışınlarını, siper engelini, bot ateşini, skor/ölüm/yeniden doğmayı ve iki tarafın da 10 öldürmede kazanmasını kontrol eder. Rapor: `Library/ArenaVerification.txt`.

Eski `ShooterRange` atış poligonu ayrı sahne olarak korunur. Build Settings'te arena ilk sıradadır.
