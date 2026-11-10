# YMH219-SpeechApp

Windows Forms masaüstü uygulaması: metni konuşmaya dönüştürür ve mikrofondan alınan konuşmayı metin olarak gösterir.

## Teknolojiler

- C# ve .NET 10 Windows Forms
- `System.Speech` konuşma sentezleme ve tanıma

## Kurulum ve çalıştırma

Windows 10/11, .NET 10 SDK ve NuGet erişimi gerekir.

```powershell
dotnet restore
dotnet run
```

Metni sol alana yazıp **Seslendir** düğmesine basın. Ses listesinden kurulu bir Windows sesini seçebilir, konuşma hızını ayarlayabilir ve metni `.txt` dosyası olarak kaydedebilirsiniz. Sağdaki **Dinlemeyi Başlat** düğmesi varsayılan mikrofonu açar. Tanınan konuşmalar sonuç alanına eklenir ve saat bilgisiyle geçmiş listesinde tutulur. **Durdur** düğmesi dinlemeyi kapatır.

## Gereksinimler ve sınırlamalar

- Ses sentezi için Windows'ta en az bir konuşma sesi kurulu olmalıdır.
- Konuşma tanıma, Windows Speech Recognition tarafından desteklenen kurulu dil paketlerine ve mikrofona bağlıdır. Türkçe tanıma, sistemde Türkçe tanıma motoru kuruluysa kullanılabilir.
- Uygulama yalnızca varsayılan mikrofonu ve kendi penceresine gönderilen ses girişini kullanır.

## Temel özellikler

- Metin kutusundaki içeriği seçili ses ve hızla seslendirme
- Mikrofon üzerinden konuşmayı metne aktarma
- Dinleme durumunu gösterme, başlatma ve durdurma
- Tanıma sonuçlarını saat damgasıyla listeleme ve geçmişi temizleme
- Tanıma geçmişini saat bilgileriyle `.txt` dosyasına aktarma
- Tanınan metni panoya kopyalama veya sonuç alanını temizleme
- Klavye kısayolları: `Ctrl+Enter` ile seslendirme, `Esc` ile dinlemeyi durdurma, `Ctrl+Shift+C` ile sonucu kopyalama
- Metni UTF-8 `.txt` dosyasına kaydetme
- Mikrofon, dil paketi ve dosya hataları için anlaşılır bildirimler

## Ekran görüntüleri

Uygulamayı yerel Windows makinesinde başlatıp ana pencerenin ve konuşma tanıma sonucunun ekran görüntülerini bu bölüme ekleyin.

## Öğrenci bilgileri

- Ad Soyad: [Adınızı yazın]
- Öğrenci Numarası: [Numaranızı yazın]
