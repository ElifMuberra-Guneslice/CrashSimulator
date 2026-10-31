# CrashSimulator — ilk sürüş prototipi

![Unity Editor kamera önizlemesi](TestTrack.png)

## Mevcut proje

İncelenen sürüm: **Unity 6000.3.26f1 (6.3 LTS)**, **URP 17.3.0**,
**Input System 1.20.0**, uGUI 2.0.0. Yeni paket eklenmedi.
Başlangıçtaki TestTrack yalnızca Main Camera, Directional Light ve Global Volume içeriyordu.
Mevcut Mobile/PC kalite profilleri ve proje genelindeki fizik/giriş ayarları korundu.
Android ve iPhone için varsayılan kalite profili zaten Mobile.

## Unity Editor'de test

1. Projeyi Unity **6000.3.26f1** ile açın; scriptlerin derlenmesini bekleyin.
2. `Assets/Scenes/TestTrack.unity` sahnesini açın. Sahne önceden açıksa diskteki
   yeni sürümü yeniden açın; eski sahneyi yeni dosyanın üzerine kaydetmeyin.
3. Hierarchy'de `CrashSimulator Test Track` altında Environment, Vehicle Spawn,
   PrototypeCar, Driving HUD ve EventSystem bulunur. Main Camera takip bileşenini taşır.
4. **Play** düğmesine basıp Game görünümüne odaklanın.

| İşlem | Klavye | Dokunmatik | Gamepad |
|---|---|---|---|
| İleri gaz | W / Yukarı | GAZ, basılı tut | Sağ tetik |
| Frenleyerek geri | S / Aşağı | GERİ, basılı tut | Sol tetik |
| Direksiyon | A-D / Sol-Sağ | SOL / SAĞ, basılı tut | Sol çubuk |
| Servis freni | Space | FREN, basılı tut | Alt yüz düğmesi |
| Başlangıca dön | R | SIFIRLA | Select |

Mobilde gaz ve direksiyona aynı anda basılabilir. Tuştan dışarı kaydırma, tuşu
bırakma, uygulamanın odağını kaybetmesi veya duraklaması basılı girişleri temizler.
Editor'de fare ile tek bir dokunmatik tuş kullanılabilir; eşzamanlı dokunmayı cihazda test edin.

Başlangıç aracı +Z yönüne, öndeki geniş beton bariyere bakar. Düz ilerleyip bariyere
çarptığınızda üstte çarpışma sayısı, yüzeye dik bağıl hız ve toplam impuls gösterilir.
İmpuls birimi N·s'dir; gösterge kuvvet veya hasar yüzdesi değildir.
Araç çarpışmadan sonra yerinde kalır; **R/SIFIRLA** ile test tekrarlanır.
Y = -8 altına düşerse otomatik başlangıca döner. Devrilme sonrasında manuel sıfırlama kullanılır.

Sıfırlama konum/dönüşü, doğrusal/açısal hızı, direksiyonu, motor torkunu, tekerlek
fizik durumunu ve çarpışma geçmişini temizler. Kamera aynı anda başlangıca geçer.
İlk 0,3 saniye fren uygulanarak süspansiyonun oturması beklenir.

## Dosya düzeni

| Yol | Sorumluluk |
|---|---|
| `Assets/Scripts/Runtime/Input/` | `VehicleInputSource` ortak sözleşmesi, klavye/gamepad/dokunmatik girişi |
| `Assets/Scripts/Runtime/Vehicle/` | Araç ayarları, WheelCollider sürüşü, çarpışma olayları ve respawn |
| `Assets/Scripts/Runtime/Camera/` | Yumuşak takip, yatay ufuk ve engelde kamerayı yaklaştırma |
| `Assets/Scripts/Runtime/UI/` | Çoklu dokunma tuşları, güvenli ekran alanı, 5 Hz hız/çarpışma göstergesi |
| `Assets/Scripts/Editor/TestTrackBuilder.cs` | Prefab/sahne oluşturma aracı; oyuncu derlemesine dahil olmaz |
| `Assets/Prefabs/Vehicles/PrototypeCar.prefab` | Rigidbody, iki gövde BoxCollider, dört WheelCollider, geometrik araç |
| `Assets/Prefabs/Environment/ConcreteBarrier.prefab` | Statik beton bariyer; Rigidbody yok |
| `Assets/Materials/Environment/` | Asphalt, Concrete, Ground, TrackPaint |
| `Assets/Materials/Vehicles/` | BodyOrange, OpaqueGlass, Rubber, Metal |
| `Assets/Settings/Vehicles/PrototypeCar.asset` | Paylaşılan araç fizik parametreleri |
| `Assets/Models/Vehicles/`, `Assets/Models/Environment/` | Sonraki model kaynakları için ayrılmış klasörler |
| `Assets/Tests/PlayMode/` | Gerçek sahne üzerinde davranış testleri |

Tüm Unity asset/klasörleri `.meta` dosyalarıyla birlikte tutulur. Runtime, Editor ve
test kodu ayrı assembly definition dosyalarındadır.

Eklenen C# dosyaları:

```text
Assets/Scripts/Runtime/Input/VehicleInputSource.cs
Assets/Scripts/Runtime/Input/PlayerVehicleInput.cs
Assets/Scripts/Runtime/Vehicle/VehicleSettings.cs
Assets/Scripts/Runtime/Vehicle/VehicleController.cs
Assets/Scripts/Runtime/Vehicle/VehicleCollisionReporter.cs
Assets/Scripts/Runtime/Vehicle/VehicleRespawn.cs
Assets/Scripts/Runtime/Camera/VehicleFollowCamera.cs
Assets/Scripts/Runtime/UI/HoldButton.cs
Assets/Scripts/Runtime/UI/SafeAreaPanel.cs
Assets/Scripts/Runtime/UI/VehicleHud.cs
Assets/Scripts/Editor/TestTrackBuilder.cs
Assets/Tests/PlayMode/VehiclePlayModeTests.cs
```

## Ayarlama ve genişletme

- `PrototypeCar.asset`: 1.200 kg kütle, tekerlek başına 650 Nm motor torku,
  2.200 Nm fren torku, 110 km/h ileri / 25 km/h geri motor kesme eşikleri,
  düşük hızda 30°, yüksek hızda 8° direksiyon.
- Dört tekerlek tahriklidir, ön ikisi yönlenir. Zıt gaz komutu araç durmaya
  yaklaşana kadar fren uygular. Motor torku hız sınırına yaklaşırken azaltılır;
  bu eşikler dış kuvvetlere veya yokuş aşağı harekete karşı mutlak hız sınırlayıcı değildir.
- Tüm fizik komutları `FixedUpdate`, görsel tekerlek ve kamera takibi `LateUpdate`
  içinde uygulanır. Unity 6 `Rigidbody.linearVelocity` API'si kullanılır.
- Tekerlek pivotları ölçeklenmez; görsel silindirler ayrı alt nesnelerdir.
  Fizik ve görsel tekerlek eşleşmeleri prefab Inspector'unda düzenlenebilir.
- Başlangıç noktasını sahnedeki `Vehicle Spawn` üzerinden taşıyın/döndürün.
  Noktayı bariyerlerin içinde veya zemin altında bırakmayın.
- Crash Lab için `VehicleInputSource` türevi bir kaynak yazıp Controller'ın `Input`
  alanına atayın. Başlangıç hızı/açısı ve test akışı ayrı bir mod yöneticisinde tutulabilir.
- Hasar, ses veya telemetri sistemi `VehicleCollisionReporter.Impact` olayına
  abone olabilir. Sürüş kodunun içine bu sistemleri eklemek gerekmez.
- Menüde **Crash Simulator → Prepare TestTrack** bulunur. Hazırlanmış sahneyi
  tekrar üretmez; mevcut düzenlemeleri korur. Teslim edilen sahne için menüyü çalıştırmak gerekmez.

## Otomatik testler

**Window → General → Test Runner → PlayMode → Run All**.
`CrashSimulator.PlayModeTests` aşağıdakileri kontrol eder:

1. Süspansiyonun oturması ve dört tekerleğin yere temas etmesi.
2. Gaz ile ilerleme ve servis freniyle durma.
3. Zıt gazın önce fren uygulayıp sonra geri sürüşe geçmesi.
4. Direksiyonun aracın yönünü değiştirmesi.
5. Beton bariyere gerçek fizik çarpışmasının raporlanması.
6. Sıfırlamada konum, hız, tekerlekler ve kameranın düzelmesi.
7. Pist altına düşmede otomatik sıfırlama.
8. Eşzamanlı dokunmatik komutlar, dışarı kaydırma ve devre dışı bırakmada temizleme.

Testler TestTrack'i açar; çalıştırmadan önce kendi sahne değişikliklerinizi kaydedin.
Bu test assembly'si test amaçlıdır ve normal oyuncu derlemesine dahil edilmez.

**20 Ekim 2026 doğrulaması:** Unity 6000.3.26f1 ile C# derlemesi başarılı;
sekiz PlayMode testi **8 geçti / 0 başarısız / 0 atlandı**. Gerçek WheelCollider
fiziğiyle test süresi yaklaşık 22 saniye. NUnit XML raporu:
`Documentation/PlayModeResults.xml`. Testler aynı kaynaklar ve sahnenin geçici
kopyasında çalıştırıldı; Android/iOS derlemesi veya fiziksel cihaz performans testi yapılmadı.

## Android / iOS kontrolü

1. **File → Build Profiles** üzerinden Android veya iOS profilini seçin.
   Gerekiyorsa Unity Hub'dan ilgili platform desteğini ayrıca kurun.
2. Profilin Scene List'ine **TestTrack** ekleyin ve ilk etkin sahne yapın.
   Projenin mevcut SampleScene build listesi bu görevde otomatik değiştirilmedi.
3. Quality bölümünde platform için mevcut **Mobile** profilini kontrol edin.
4. Geliştirme derlemesini gerçek cihazda çalıştırın. iOS için Xcode/imzalama
   ortamınız, Android için kurulu Android araç zinciriniz gerekir.
5. Gaz + direksiyon, gaz + fren, sıfırlama, uygulamayı arka plana alıp geri dönme,
   ekran güvenli alanları ve art arda çarpışmaları deneyin.
6. Development Build ve Unity Profiler ile hedef cihazda CPU Physics, GPU,
   UI ve GC Alloc değerlerini ölçün. Cihaz ölçümü yapılmadan sabit FPS garantisi yoktur.

Geometri yerleşik ve düşük ayrıntılıdır; harici doku/model kullanılmaz. Paylaşılan
opak URP Simple Lit materyalleri, statik pist nesneleri, tek ışık ve basit colliderlar
kullanılır. Araçta Continuous Dynamic çarpışma algılama ve Rigidbody interpolation
vardır; diğer nesnelere gereksiz Rigidbody eklenmez. Araç süspansiyonu düşük hızda
3, yüksek hızda 1 alt adım kullanır. Kamera sorguları önceden ayrılmış bir hit tamponu
kullanır; HUD metni 5 Hz güncellenir. Sahnenin kamera post-processing'i kapalıdır.

Bu ilk teslim, sert gövdeli bir sürüş/çarpışma prototipidir. Mesh deformasyonu,
parça kopması, hasar modeli, ses, lastik izi, tamamlanmış Free Drive/Crash Lab menüleri
ve fiziksel olarak kalibre edilmiş araç davranışı sonraki aşamalardır.
