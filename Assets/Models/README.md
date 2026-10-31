# Model kaynakları

Araç kaynak modelleri `Vehicles/` altına glTF kaynakları ve orijinal buffer/doku/lisans
dosyalarıyla eklendi. Unity glTFast 6.20.0 paketi `Packages/manifest.json` içinde
tanımlı. Unity Editor paketi çözümleyip modelleri içe aktarınca kaynaklar Unity model
varlıklarına dönüşür; henüz fizik/oyun prefabı olarak yapılandırılmış değiller.

- `Vehicles/LucidAirSapphire2024/`: detaylı görsel model. Mobil için optimizasyon ve
  Sketchfab Standard lisans koşulları kontrol edilmeli.
- `Vehicles/LowPolySedan/`: düşük poligonlu model, CC BY 4.0 atıfı gerekli.
- Ford F-150 modelini kullanıcı isteğiyle projeye dahil etmedik.
- `Environment/`: bariyer/pist model kaynakları.

Araç eksenleri: +Z ileri, +Y yukarı, +X sağ; 1 Unity birimi = 1 metre.
Modeli değiştirirken Rigidbody kökünün ölçeğini (1,1,1) koruyun. Tekerlek görsellerinin
mesh alt nesnesi döndürülebilir; WheelCollider nesnelerinin yerel dönüşü sıfır olmalıdır.
Fizik için basit colliderlar kullanın; görsel modelin ayrıntısını fiziğe taşımayın.
