# Phase 2 — hasar sistemi

## Başlangıç incelemesi

- Unity 6000.3.26f1, URP 17.3, Input System 1.20. Mevcut proje korunur.
- `VehicleController`: 1200 kg dinamik Rigidbody, Continuous Dynamic, interpolation,
  8/2 solver iteration. Kütle merkezi Awake sırasında `(0, 0.45, 0)` ayarlanır.
- Dört WheelCollider: 0,34 m yarıçap, 0,22 m süspansiyon, tekerlek başına 20 kg;
  ön tekerlekler yönlenir, dört tekerlek motor torku alır.
- `VehicleCollisionReporter` eski hız/impuls olayını korur; analiz verisi için yeni
  olay sözleşmesi eklenir. Çarpışma analizinden görsel/mekanik güncellemeler ayrılır.
- `VehicleRespawn` konum/hız/tekerlekleri sıfırlar ve `Respawned` olayını yayınlar.
- `VehicleFollowCamera` mevcut hedef takibini ve engel kontrolünü korur.
- PrototypeCar'ın gövde ve kabini basit BoxCollider; görseller yerleşik Cube/Cylinder.
  Yerleşik Cube az köşeli olduğundan lokal göçük için uygun değildir. Gövde görseli
  ilerleyen aşamada yerel olarak üretilmiş bölümlenmiş bir mesh ile yükseltilir;
  fizik colliderı basit kalır.
- TestTrack, PrototypeCar ve ConcreteBarrier referansları mevcut.

## Çarpışma verisi ve birimler

`CollisionData` her araç için bağımsız bir değer kopyasıdır: bağıl hız (m/s), toplam
impuls (N·s), dünya/yerel temas konumu ve normali, gövdeye doğru etki yönü, bölge,
oyun puanı ve fizik zamanı. En fazla sekiz temas saklanır; gerçek temas sayısı da
ayrıca tutulur. Puan fiziksel yaralanma, NCAP veya güvenlik derecelendirmesi değildir.

Hız bileşeni temas normaline izdüşürülür; yanından sürtünerek geçiş tam hızdaki
önden çarpışma gibi değerlendirilmez. İmpuls araç kütlesine bölünerek ek bir oyun
şiddeti girdisi elde edilir. Bölge eşiği ve yapısal direnç hasara dönüşümü belirler.

Temas noktaları yeni dizi üretmeden `Collision.GetContact` ile okunur. Her çarpılan
Rigidbody (statik nesnelerde ImpactSurface veya collider) için 32 girişlik sınırlı
tekrar filtresi vardır. Birden fazla temas aynı olay içindedir. Sürekli temasın
`OnCollisionStay` çağrıları yeniden hasar üretmez; ayrılıp yeniden çarpışma, eşik ve
bekleme süresini geçince yeni olaydır. Çok kalabalık temaslarda filtre kapasitesi
ve zaman aralığı Inspector/profil sınırları dikkate alınarak test edilmelidir.

API kaynakları: [Unity Collision.GetContact](https://docs.unity3d.com/ScriptReference/Collision.GetContact.html),
[Unity Collision.impulse](https://docs.unity3d.com/ScriptReference/Collision-impulse.html).

## Uygulanan yapı

- `VehicleDamage` altı ana bölgeyi ayrı izler; köşeler, kapılar, kaput, bagaj ve
  tekerlekler için isteğe bağlı yerel sınır bölgeleri vardır. Aynı anda birden
  fazla bölge hasarlanabilir. `DamageZone` eşik, direnç, üst sınır ve görsel durum
  tutar.
- `DeformableVehicleMesh`, yaklaşık 1.030 köşeli basit bir bölümlenmiş gövde
  mesh'inin yalnız yakın hücrelerini değiştirir. Etki başına 256 köşe sınırı
  vardır. Çalışma zamanında mesh'in özel kopyası alınır; prefab mesh'i değişmez.
  BoxCollider sabit kalır. Reset orijinal köşeleri geri yükler.
- `DamageStateMesh` hafif/orta/ağır mesh değişimi için çalışır; hasarlı sanat
  mesh'leri bulunmadığından bu alanlar boş bırakılmıştır. Geometrik göçük
  prototipte görülebilir.
- Bumper, kaput, bagaj, kapılar ve aynalar yerel hasar eşiğiyle kopabilir.
  Tekerlek kopması Inspector'da bilinçli olarak açılabilir; varsayılan kapalıdır.
  Parçalar 8 aktif debris/12 saniye bütçesiyle sınırlandırılır, araç collider'ı
  ile çarpışmaları yoksayılır, reset sırasında asıl ebeveyne döner.
- `MechanicalDamage`, bölgeye göre motor gücü, fren, direksiyon sapması,
  sürtünme ve ilave drag üretir. Controller yalnız `IVehicleMechanicalStatus`
  arayüzünü bilir; başka araç kontrolcüsü aynı arayüzü uygulayabilir.
- `SideImpactScenario`: iki 1200 kg dinamik araç; A, 90 km/sa = 25 m/s başlangıç
  hızıyla B'nin yanına vurur. B'nin pozisyonu ve hareketi çarpışma sonrası
  script tarafından zorlanmaz. Oyuncu `90 km/h YAN TEST` ile testi başlatır,
  `TESTI SIFIRLA` ile iki aracı ve hasarı geri alır, `HEDEFI INCELE` veya
  `ARACI INCELE` ve dokunma sürükleme/sağ fare tuşuyla aracı inceler.
- `ImpactEffects` araç başına tek yeniden kullanılan ParticleSystem kullanır:
  kıvılcım/metal, beton tozu, kabin collider'ında cam parçacığı, hasarlı teker dumanı ve motor dumanı
  için renk/boyut varyasyonları. Her emisyon ve toplam parçacık sayısı sınırlı.
  Bunlar geçici geometrik efektlerdir, gerçekçi VFX varlığı iddia edilmez.
- `ImpactAudio` üç AudioSource'u yeniden kullanır. Light/medium/heavy, glass,
  metal ve part clip alanları Inspector'da mevcuttur; ücretsiz/özgün ses dosyası
  henüz eklenmediğinden şu anda sessiz çalışır.
- `DamageReport` son 32 çarpışmayı ve toplam sayıyı, ana bölgeyi, hız/impulsu,
  hasar puanını, kopan parçaları ve mekanik durumu tutar. `CrashReportHud` bu
  verinin yalnızca 5 Hz yenilenen sunum katmanıdır. Skor güvenlik derecesi değildir.

## Unity Editor'de açma ve test etme

1. Unity Hub'da **CrashSimulator** projesini Unity **6000.3.26f1** ile açın.
   Project panelinde **Assets > Scenes > TestTrack** sahnesine çift tıklayın.
   Sahne/prefab yükseltmesi kaydedilmiştir. Gerekirse üst menüde
   **Crash Simulator > Phase 2 > Finish Crash Lab** idempotent kurulumunu çalıştırın.
2. `PrototypeCar` prefabında `VehicleDamage`, `DamageReport`, `MechanicalDamage`,
   `ImpactEffects`, `ImpactAudio`, `ImpactSurface` ve gövdede
   `DeformableVehicleMesh` bulunmalı. `MobileDamage.asset` kalite bütçelerini
   yönetir. `VehicleController` dört WheelCollider ve 1200 kg Rigidbody ile
   çalışır; prefabda WheelCollider veya kütle ayarı değiştirmeyin.
3. Play'e basın. W/S gaz/geri, A/D direksiyon, Space fren, R oyuncu aracını
   sıfırlar; dokunmatik sürüş butonları da çalışır. Üstteki
   **90 km/h YAN TEST** butonuyla iki araçlı deneyi başlatın. **HASAR RAPORU**
   butonuyla sonuçları açın. Testin tamamını sıfırlamak için
   **TESTI SIFIRLA** kullanın.
4. Test Runner'da EditMode ve PlayMode gruplarını çalıştırın. PlayMode
   çarpışma testleri birkaç saniye fizik simülasyonu gerektirir.

## Doğrulama ve manuel kontrol listesi

Unity 6000.3.26f1 başsız Editor ortamında 21–25 Ekim 2026 tarihinde:

- Son EditMode çalışması: **19/19 geçti**. Temas kopyası, eşik/şiddet,
  bölgeleme, tekrar filtresi, deformasyon/restore, parçalar ve mekanik model.
- 25 Ekim son tam PlayMode çalışması: **14/14 geçti**. Phase 1 sürüş regresyonu ile
  düşük/yüksek hız ön bariyer darbesi, yan araç çarpışması, arka bariyer
  darbesi, ardışık çarpışmalar, parça kopması, rapor ve reset dahil.
- Son tam EditMode ve PlayMode çalışmaları kamera inceleme girdisi ve teker
  dumanı kodu eklendikten sonra yapıldı; kabin cam kategorisi eklendikten sonra
  PlayMode yeniden çalıştırıldı. Derleme hatası yoktu.

Editor Game görünümünde ayrıca şu kontrolleri elle yapın: soldan vurulan
aracın sağ tarafı göçmemeli; hafif sürüşte parça kopmamalı; çoklu darbeler
raporda artmalı; sıfırlamadan sonra kopan parçalar/köşeler/tekerlekler geri
gelmeli; parçalar aracı fırlatmamalı. Testi Android ve iOS cihazlarında
30/60 FPS, GC alloc, ısınma ve bellek için ayrıca profilleyin. Başsız Editor
testleri görsel kaliteyi veya gerçek cihaz performansını doğrulamaz.

İstenen test kapsamı ve halen gereken elle doğrulama:

| Durum | Otomatik sonuç | Elle kontrol |
| --- | --- | --- |
| Düşük/yüksek hızlı ön darbe | İkisi ve şiddet sıralaması geçti | Göçük görünümü |
| Yan ve araçtan araca 90 km/sa darbe | İki dinamik araçta hasar ve B hareket/dönüş geçti | Görsel yön/doğallık |
| Arka darbe | Arka bölge kaydı geçti | Bagaj kopma eşiği |
| Ardışık darbeler | İkinci darbe ayrı kaydedildi | Uzun test oturumu |
| Kopabilir parça | Yan darbede parça kopması geçti | Parçanın görünür davranışı |
| Mesh restore | Köşe verileri EditMode'da, PlayMode reset durumu geçti | Görsel karşılaştırma |
| Mekanik toparlanma | Bölgeye özgü model ve reset sonrası tam çarpan test edildi | Hasarlı motorla sürüş + reset |
| Rapor doğruluğu | Sayı, hız, bölge ve impuls PlayMode'da denetlendi | HUD okunabilirliği |
| Normal sürüşte hasar yok | Sürüş/oturma testlerinde geçti | Uzun süreli sürüş |
| Tekrarlı reset | Üç reset ve debris sayacı geçti | Mobil cihazda tekrar |

Özellikle telefon FPS/GC ölçümü ve ekran görüntüsüne dayalı görsel kalite kontrolü
bu ortamda yapılmadı. Bunlar cihaz ve Unity Game görünümü üzerinden yapılmalıdır.

## Sınırlar ve sonraki aşama

Hasar görsel mesh'i temsilidir; fizik BoxCollider şekli deforme olmaz. Cam
ve metal parçacıkları bir ParticleSystem üzerinde farklı stillerle sunulur.
Ses ve hasarlı mesh varyantları için lisansı uygun özgün içerik gerekir.
Tam replay kaydı, yaralanma/safety skoru ve soft-body fizik yoktur.
Sonraki aşama: cihaz profili sonrası özgün düşük poligonlu gövde/parça
mesh'leri, ücretsiz/özgün çarpışma sesleri ve VFX iyileştirmesi.

## Oluşturulan ve değiştirilen dosyalar

Yeni çalışma zamanı dosyaları: `Assets/Scripts/Runtime/Damage/` içindeki
`CollisionData.cs`, `ImpactAnalyzer.cs`, `CollisionSeverityCalculator.cs`,
`ImpactSurface.cs`, `DamageZone.cs`, `DamageQualitySettings.cs`,
`VehicleDamage.cs`, `DeformableVehicleMesh.cs`, `DamageStateMesh.cs`,
`DetachablePart.cs`, `MechanicalDamage.cs`, `SideImpactScenario.cs`,
`ImpactEffects.cs`, `ImpactAudio.cs`, `DamageReport.cs`; ayrıca
`Assets/Scripts/Runtime/Vehicle/IVehicleMechanicalStatus.cs` ve
`Assets/Scripts/Runtime/UI/CrashReportHud.cs`.

Yeni Editor kurucuları: `Assets/Scripts/Editor/Phase2Setup.cs`,
`Phase2FinalSetup.cs`. Yeni testler: `Assets/Tests/EditMode/` altında
`CollisionAnalysisTests.cs`, `DamageZoneTests.cs`, `DeformationTests.cs`,
`DetachablePartTests.cs`, `MechanicalDamageTests.cs`; `Assets/Tests/PlayMode/`
altında `CrashLabPlayModeTests.cs`. Unity `.meta` dosyaları da vardır.

Oluşturulan varlıklar: `Assets/Models/Vehicles/DeformableChassis.asset`,
`Assets/Settings/Vehicles/MobileDamage.asset`. Güncellenen varlıklar:
`Assets/Prefabs/Vehicles/PrototypeCar.prefab`,
`Assets/Prefabs/Environment/ConcreteBarrier.prefab`,
`Assets/Scenes/TestTrack.unity`. Güncellenen mevcut kod:
`VehicleController.cs`, `VehicleCollisionReporter.cs`, `VehicleRespawn.cs`,
`VehicleFollowCamera.cs`, `VehiclePlayModeTests.cs`.
