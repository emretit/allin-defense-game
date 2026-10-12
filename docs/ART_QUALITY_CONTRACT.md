# ALLIN: Defense — Art Quality Contract

Bu belge, `img2threejs` makro → meso → mikro inceleme ve açık görsel QA
yaklaşımının ALLIN: Defense için uyarlanmış halidir. Hedef görüntü:
`docs/art/defender-quality-target-v1.png`.

## Referans uygunluğu

- Referans bu proje için OpenAI image generation ile üretilmiş özgün bir iç sanat
  hedefidir; başka bir oyundan mesh, doku veya ekran görüntüsü içermez.
- Tek görüntü yalnız kamera, palet, ışık ve okunabilirlik hedefini tarif eder. Gizli
  yüzler ve tam geometri Blender kaynaklarında özgün olarak tasarlanır.
- Hedef stilize ve mobil-düşük poligondur; birebir offline-render ayrıntısı vaat
  edilmez.

## Makro kontrat

- Harita ilk bakışta düşman kapısı → kıvrımlı yol → savunma kalesi akışını anlatır.
- Kamera, kule ve aktörlerin yan yüzlerini gösterecek kadar alçak; tüm savunma
  düğümlerini gösterecek kadar yüksek kalır.
- Soğuk ay ışığı sahnenin ana değer yapısını, turuncu kapı ve camgöbeği büyü ise
  iki kontrollü vurgu kutbunu oluşturur.
- HUD dünya görüntüsünü çerçeveler; ana oyun alanını koyu, boş bir dikdörtgene
  dönüştürmez.

## Meso kontrat

- Yol sıcak kahverengi, aşınmış ve taş bordürlü okunur; kale taşıyla aynı gri
  yüzeye dönüşmez.
- Zemin en az üç değer katmanı taşır: ana orman, açık çim yamaları ve koyu sınır.
- Her karakol ailesi çatı/tepe siluetiyle ayrılır; seviye 2 gerçek bir yapı
  yükseltmesidir.
- Aktörlerde takım rengi + açık nötr zırh kullanılır. Gövdenin yarısını yutan saf
  siyah ikincil malzeme yasaktır.
- Kapı ve kale, haritadaki diğer yapılardan en az 1,5× görsel kütleye sahiptir.

## Mikro kontrat

- Ana yüzlerde tek segment bevel, kontrollü pürüzlülük ve küçük malzeme değeri
  ayrımı kullanılır.
- Ağaç, kaya, yol taşı, çim/kır çiçeği ve rün kümeleri deterministik yerleşir;
  oynanış yoluna taşmaz.
- Ateş ve büyü emissive değerleri bloom üretir ancak şekli beyaza patlatmaz.
- Gerçek zamanlı gölge aktörleri zemine bağlar; eski siyah disk gölgeler çizilmez.

## Mobil bütçe

- Aktör/karakol başına en fazla iki paylaşılan materyal.
- Tek ana gölgeli yön ışığı; dolgu ışıkları gölgesiz.
- SMAA Medium, hafif bloom/ACES/renk düzenleme. iOS uGUI subpass uyumu için
  ayrıca MSAA kullanılmaz.
- Çevre ayrıntıları düşük poligon ve tekil collider’sızdır.

## İnceleme kapıları

Her kabul görüntüsünde hazırlık, yoğun dalga, boss ve sonuç hali şu sırayla
incelenir: yol akışı, değer hiyerarşisi, takım silueti, kule ailesi, temas gölgesi,
HUD taşması. Her turda karar yalnız `continue`, `refine-spec`, `refine-code`,
`request-input` veya `stop` olur; kalan kusur açıkça kaydedilir.
