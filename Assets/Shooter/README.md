# Kübik atış poligonu

Unity 2022.3, Built-in Render Pipeline. Ek paket veya dış varlık gerektirmez.

`Assets/Scenes/ShooterRange.unity` sahnesini açıp **Play** tuşuna basın. Oyun alanına tıklayın.

| Tuş | İşlev |
| --- | --- |
| Fare | Nişan al |
| Sol tık / basılı tut | Ateş et |
| R | Şarjör değiştir |
| WASD | Hareket et |
| T | Mühimmatı yenile |
| Esc | Fare imlecini serbest bırak |

- Şarjör: 12, başlangıç yedeği: 120 mermi. Atış aralığı: 0,16 sn. Şarjör değiştirme: 1,35 sn.
- Dolu şarjörle veya yedek mermi olmadan şarjör değiştirilmez. Değişim sırasında ateş edilmez.
- Kısmi değişimde sadece eksik mermi kadar yedek tüketilir. Boş şarjörde ateş etmek mermi sayısını azaltmaz.
- Hedefler üç isabette düşer ve 2,2 saniye sonra yeniden kurulur. İsabet ve düşen hedef sayısı HUD'da görünür.
- Silah modeli, atış ışığı, iz çizgisi, isabet efekti, geri tepme ve sentezlenmiş sesler dahildir.
- `Player > Range Weapon` bileşeninden kapasite, yedek mermi, atış aralığı ve değişim süresi ayarlanabilir.
- `Range Player` bileşeninden hareket hızı ve fare hassasiyeti ayarlanabilir.
- `Shooter > Atış poligonunu aç` menüsü sahneyi açar. Örnek SampleScene korunur.
- `Shooter > Mekanikleri doğrula` menüsü Play modunda gerçek atış ve şarjör testlerini çalıştırır; rapor `Library/ShooterVerification.txt` dosyasına yazılır.

Kodlar: `Assets/Shooter/Scripts`. Sahne oluşturma ve doğrulama araçları: `Assets/Shooter/Editor`.
