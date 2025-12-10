Move2Germany: Teknik Keşif, Kapsamlı Mimarî Tasarım ve Stratejik Ürün Geliştirme Raporu




1. Yönetici Özeti ve Stratejik Vizyon


Küresel iş gücü hareketliliğinin arttığı, demografik değişimlerin Avrupa ekonomilerini nitelikli göçmen çekmeye zorladığı bir dönemde, Almanya'ya göç süreci; akademik hedefler, kariyer arayışları ve aile birleşimi gibi hayati motivasyonlarla şekillenen karmaşık bir yolculuktur. "Move2Germany" girişimi, bu süreci yalnızca dijitalleştirmeyi değil, aynı zamanda insan odaklı bir yaklaşımla dönüştürmeyi hedefleyen kapsamlı bir teknolojik çözüm olarak tasarlanmıştır. Bu rapor, React 18 tabanlı, Supabase altyapısı üzerinde çalışan ve yapay zeka destekli Edge Functions ile güçlendirilmiş, konfigürasyon güdümlü (config-driven) bir mimariye sahip bu uygulamanın derinlemesine teknik keşif ve stratejik planlama sürecini belgelemektedir.
Projenin temel amacı, Almanya'nın bürokratik karmaşıklığını (örneğin, Mavi Kart maaş eşikleri, bloke hesap limitleri, konut piyasası dinamikleri) kullanıcı için şeffaf, yönetilebilir ve psikolojik açıdan sürdürülebilir bir deneyime dönüştürmektir. Bu rapor, yasal çerçevenin teknik gereksinimlere nasıl tercüme edileceğini, veri güvenliğinin GDPR standartlarında nasıl sağlanacağını ve yapay zekanın halüsinasyon risklerini minimize ederek nasıl entegre edileceğini detaylandırmaktadır. Rapor boyunca, uygulamanın mimarisi, değişen yasalar karşısında kod tabanını yeniden dağıtmaya gerek kalmadan adaptasyon sağlayacak esnek bir yapı üzerine kurgulanmıştır.
________________


2. Almanya Göç Yasası ve Düzenleyici Çerçeve Analizi


Uygulamanın iş mantığının (business logic) temelini, Almanya'nın sıkı ve sürekli güncellenen göç yasaları oluşturmaktadır. Bu bölüm, yazılımın "karar motoru"nun (decision engine) nasıl çalışması gerektiğini belirleyen yasal parametreleri analiz etmektedir.


2.1 AB Mavi Kart (EU Blue Card) 2025 Düzenlemeleri ve Maaş Eşikleri


AB Mavi Kart, akademik geçmişe sahip nitelikli çalışanlar için Almanya'ya girişin ana anahtarıdır. Ancak 2025 yılı itibarıyla yapılan düzenlemeler, yazılımın doğrulama algoritmalarında köklü değişiklikler gerektirmektedir. Geleneksel "tek tip vize" yaklaşımının aksine, sistemin kullanıcının profiline göre dinamik olarak değişen maaş eşiklerini doğrulaması gerekmektedir.
2025 yılı itibarıyla, genel meslek grupları için brüt yıllık maaş eşiği 48.300 Euro olarak belirlenmiştir.1 Bu rakam, uygulamanın "Uygunluk Hesaplayıcı" modülünde bir sabit (constant) olarak değil, sunucu tarafından yönetilen bir konfigürasyon değişkeni olarak tutulmalıdır. Zira bu rakamlar her yıl ekonomik göstergelere göre güncellenmektedir.
Ancak, yazılım mimarisi açısından daha karmaşık olan durum, "darboğaz meslekler" (MINT: Matematik, Bilişim, Doğa Bilimleri, Teknoloji ve tıp meslekleri) ile "yeni mezunlar" için uygulanan indirimli eşiktir. Bu grup için 2025 eşiği 43.759,80 Euro olarak saptanmıştır.1 Bu durum, uygulamanın veritabanı şemasında kullanıcının mesleğini ve mezuniyet tarihini hassas bir şekilde saklamasını zorunlu kılar. Örneğin, sistemin bir kullanıcının mezuniyet tarihinin üzerinden tam olarak 36 ay geçip geçmediğini hesaplaması kritik bir önem taşır; çünkü 37. ayda başvuran bir aday, artık "yeni mezun" statüsünde değerlendirilmez ve yaklaşık 4.500 Euro daha yüksek bir maaş teklifi sunmak zorunda kalır.3 Bu ince ayrım, vize reddi ile onayı arasındaki farkı belirlediğinden, tarih hesaplama fonksiyonlarının (örneğin date-fns veya dayjs kütüphaneleri kullanılarak) yüksek hassasiyetle kurgulanması gerekir.


2.1.1 Diplomasız BT Uzmanları İçin Özel Mantık


Alman göç yasasındaki en devrimci değişikliklerden biri, üniversite diploması olmayan ancak sektör deneyimi olan BT uzmanlarına Mavi Kart yolunun açılmasıdır. Bu kullanıcı grubu için uygulamanın akış şeması (flow chart) tamamen farklılaşmalıdır. Sistem, bu kullanıcılardan diploma yerine şu kanıtları talep edecek bir arayüz sunmalıdır:
1. Deneyim Süresi: Son yedi yıl içinde edinilmiş en az üç yıllık BT deneyimi.1
2. Maaş Eşiği: Darboğaz meslekler için geçerli olan indirimli eşik (43.759,80 Euro).3
3. Seviye Tespiti: Deneyimin üniversite düzeyinde (ISCED 2011 Seviye 6) olduğunun kanıtlanması.2
Bu gereksinimler, uygulamanın "Profil Oluşturma" aşamasında dinamik bir form yapısı gerektirir. Kullanıcı "BT Uzmanıyım ancak diplomam yok" seçeneğini işaretlediğinde, React tabanlı ön yüz (frontend), diploma yükleme bileşenini (component) gizleyip, yerine detaylı bir "Proje ve Deneyim Dökümü" formu yüklemelidir.


2.2 Nitelikli İş Gücü Göçü Yasası (FEG) ve §81a Hızlandırılmış Prosedür


Uygulamanın en karmaşık modüllerinden biri, İşveren, Yabancılar Dairesi (Ausländerbehörde) ve Federal İş Ajansı (BA) arasındaki üçlü etkileşimi yöneten §81a "Hızlandırılmış Nitelikli İş Gücü Prosedürü"dür.4
Bu süreç, standart vize başvurularından farklı olarak, işverenin Almanya içinden süreci başlatmasını gerektirir. Move2Germany uygulaması, bu süreci dijital bir "Durum Makinesi" (State Machine) olarak modellemelidir. Süreç adımları şunlardır:
1. Yetkilendirme: Adayın işverene vekalet vermesi.
2. Anlaşma: İşverenin Yabancılar Dairesi ile hızlandırılmış prosedür anlaşması imzalaması.
3. Denklik: ZAB (Yabancı Eğitim Merkezi) üzerinden denklik sürecinin başlatılması (maksimum 2 ay).6
4. Ön Onay (Vorabzustimmung): İş Ajansı ve Yabancılar Dairesi'nin onayı.
5. Vize Başvurusu: Adayın bu ön onayla konsolosluğa gitmesi.4
Uygulamanın "İşveren Portalı" veya adayın işvereniyle senkronize olabileceği bir "Paylaşımlı Takip Ekranı" sunması, sürecin şeffaflığını artıracaktır. Genellikle adaylar, işverenlerinin Almanya'da hangi aşamada olduğunu bilemedikleri için büyük bir stres yaşamaktadırlar. Uygulama, işverenin sisteme girdiği "Ön Onay Belgesi"ni (PDF) otomatik olarak adayın hesabına yansıtarak bu bilgi asimetrisini çözmeyi hedeflemelidir.


2.3 Öğrenciler İçin Finansal Yeterlilik ve Bloke Hesap


Öğrenci vizesi başvurularında en kritik nokta "Bloke Hesap" (Sperrkonto) zorunluluğudur. 2025 yılı için belirlenen yasal tutar yıllık 11.904 Euro olup, bu tutar aylık 992 Euro kullanım hakkına denk gelmektedir.7
Ancak uygulama, kullanıcıya sadece "11.904 Euro hazırlayın" dememelidir. Araştırmalar, Fintiba veya Care Concept gibi hizmet sağlayıcıların kurulum ücretleri (setup fees) ve aylık işletim ücretleri ile birlikte toplam maliyetin 12.050 - 12.150 Euro bandına çıktığını göstermektedir.10 "Gerçek Maliyet Hesaplayıcı" modülü, bu gizli maliyetleri de içererek öğrencinin vize görüşmesinde eksik bakiye sürpriziyle karşılaşmasını engellemelidir. Ayrıca, bu tutarın BAföG oranlarına endeksli olduğu 11 bilgisini vererek, kullanıcılara bu rakamın her kış döneminde artabileceği uyarısını yapmalıdır.
________________


3. Kullanıcı Psikolojisi ve "Sakin Tasarım" (Calm Design) İlkeleri


Teknik mimari ne kadar güçlü olursa olsun, hedef kitlenin içinde bulunduğu psikolojik durum göz ardı edilirse ürün başarısız olacaktır. Göç süreci, belirsizlik ve yüksek stres içeren bir deneyimdir.


3.1 Göçmenlerin "Acı Noktaları" ve Duygu Durumu


Son yapılan anketler, Almanya'daki yabancıların iyimserlik oranlarında ciddi bir düşüş ( %14'ten %7'ye) olduğunu göstermektedir.12 Kullanıcıların en büyük şikayetleri "ruh emici bürokrasi", dijitalleşme eksikliği ve sosyal izolasyondur.13 Özellikle konut bulma süreci (%64 memnuniyetsizlik oranı ile) ve dil bariyeri, kullanıcıların kendilerini çaresiz hissetmelerine neden olmaktadır.13
Bu veriler, Move2Germany uygulamasının sadece bir "bilgi portalı" değil, aynı zamanda bir "dijital yol arkadaşı" olması gerektiğini ortaya koymaktadır. Kullanıcı arayüzü (UI), kullanıcının kaygı seviyesini yönetmek üzerine kurgulanmalıdır.


3.2 Sakin Teknoloji (Calm Tech) Prensipleri


Yüksek stres altındaki kullanıcıların bilişsel kapasiteleri azalır. Bu nedenle, uygulama tasarımında şu prensipler benimsenmelidir:


3.2.1 Aşamalı İfşa (Progressive Disclosure)


Kullanıcıya 18 adımlık vize sürecinin tamamını tek seferde göstermek, "analiz felci"ne (analysis paralysis) yol açabilir.14 Bunun yerine, arayüz karmaşıklığı sadece gerektiğinde açığa çıkarmalıdır.
* Aşama 1: Sadece "Uygunluk Kontrolü".
* Aşama 2: Uygunluk onaylandıktan sonra "Evrak Toplama".
* Aşama 3: Evraklar tamamlandıktan sonra "Randevu Alma".
Bu yöntem, kullanıcının odaklanmasını sağlar ve sürecin göz korkutuculuğunu azaltır.15


3.2.2 Empatik Dil ve Ton


Hata mesajları ve sistem bildirimleri, teknik jargon yerine insani ve empatik bir dil kullanmalıdır. "Geçersiz Girdi" yerine "Bu tarihi doğrulayamadık, formatı kontrol edebilir misiniz?" gibi yumuşak ifadeler tercih edilmelidir.14 Renk paletinde, tehlike sinyali veren parlak kırmızılar yerine, sakinleştirici mavi ve yeşil tonları ile uyarı için yumuşak amber tonları kullanılmalıdır.16 Bu, Yerkes-Dodson yasasına uygun olarak, kullanıcının uyarılma seviyesini optimal düzeyde tutmaya yardımcı olur.


3.3 Topluluk Güvenliği ve Moderasyon


Uygulama içindeki forumlar, sosyal izolasyonla mücadele için kritiktir. Ancak moderasyonsuz expat forumları hızla toksik bir ortama dönüşebilmektedir. Move2Germany, "kendi araştırmanı yap" (Do your own research) düşmanlığını engelleyen, destekleyici bir topluluk kuralları dizisi benimsemelidir.17
Otomatik Moderasyon: Yapay zeka destekli algoritmalar, nefret söylemi, ırkçı ifadeler ve kişisel veri (PII) sızıntılarını anlık olarak tespit etmeli ve engellemelidir.18 Özellikle konut dolandırıcılığı bağlamında, kullanıcıların telefon numaralarını veya adreslerini herkese açık paylaşmaları engellenmelidir.20
________________


4. Frontend Mimarisi: Config-Driven UI ve React 18 Stratejisi


Uygulamanın en kritik teknik kararı, arayüzün (UI) iş mantığından ayrıştırılmasıdır. Almanya'daki yasaların sık değişmesi (örneğin, Mavi Kart maaşının her yılın başında değişmesi), kod tabanına (hardcoded) gömülü değerlerin sürekli güncellenmesini ve uygulamanın yeniden derlenip (build) uygulama marketlerine gönderilmesini gerektirir. Bu operasyonel yükü ortadan kaldırmak için Config-Driven UI (Konfigürasyon Güdümlü Arayüz) mimarisi benimsenmiştir.


4.1 JSON Şema Stratejisi ve Akış Yönetimi


Bu mimaride, vize başvuru süreçleri veya formlar React bileşenleri olarak değil, sunucudan (Supabase) çekilen JSON nesneleri olarak tanımlanır.21
Örneğin, bir BT Uzmanı için Mavi Kart akışı şu şekilde bir JSON yapısı ile tanımlanabilir:


JSON




{
 "flow_id": "blue_card_it_2025",
 "version": "1.2",
 "steps":
     },
     "conditions": {
       "step_completed": "salary_check"
     }
   }
 ]
}

Bu yapı sayesinde, hükümet maaş eşiğini değiştirdiğinde veya yeni bir belge zorunluluğu getirdiğinde, geliştirici ekibin tek yapması gereken veritabanındaki JSON dosyasını güncellemektir. Uygulama, bir sonraki açılışta yeni konfigürasyonu çeker ve arayüzü anında günceller.22


4.2 Ziyaretçi Deseni (Visitor Pattern) ile Dinamik Render


React tarafında bu JSON'u işlemek için bir "FlowRenderer" bileşeni geliştirilecektir. Bu bileşen, Ziyaretçi Tasarım Deseni'ni (Visitor Pattern) kullanarak JSON ağacını dolaşır ve her bir düğüm (node) için uygun React bileşenini dinamik olarak yükler.23


JavaScript




const ComponentRegistry = {
 SalaryInput: React.lazy(() => import('./components/SalaryInput')),
 FileUpload: React.lazy(() => import('./components/FileUpload')),
 //... diğer bileşenler
};

const FlowRenderer = ({ schema }) => {
 return schema.steps.map((step) => {
   const Component = ComponentRegistry[step.component];
   return (
     <Suspense fallback={<SkeletonLoader />}>
        <Component {...step.props} validation={step.validation} />
     </Suspense>
   );
 });
};

Bu yaklaşım, react-jsonschema-form kütüphanesinin sunduğu yeteneklerin ötesine geçerek, formlar arası karmaşık geçiş mantıklarını (örneğin; "Maaş yeterli ama diploma yoksa, BT deneyim ekranına yönlendir") yönetmemizi sağlar.24


4.3 Performans Optimizasyonu ve Kod Bölme (Code Splitting)


Uygulama, üniversite arama motorundan konut analizine, vize hesaplayıcıdan yapay zeka sohbetine kadar geniş bir yelpazeyi kapsadığından, "Bundle Size" (Uygulama Boyutu) hızla şişebilir. Bu sorunu yönetmek için agresif kod bölme stratejileri uygulanmalıdır.


4.3.1 Rota Bazlı Bölme (Route-Based Splitting)


Kullanıcı tipleri (Persona) birbirinden çok farklıdır. Bir "Öğrenci" kullanıcısı, muhtemelen asla "Mavi Kart Aile Birleşimi" modülünü kullanmayacaktır. Bu nedenle, React Router kullanılarak her ana modül (/student, /blue-card, /researcher) ayrı bir "chunk" olarak paketlenmelidir.25 Böylece öğrenci sadece öğrenci modülünü indirir, bu da ilk yükleme süresini (Time to Interactive) radikal biçimde düşürür.


4.3.2 Bileşen Bazlı Bölme


Ağır görselleştirme kütüphaneleri (örneğin, Almanya haritası üzerinde yaşam maliyeti ısı haritası) veya karmaşık grafikler, sadece kullanıcı o ekrana geldiğinde (Intersection Observer API kullanılarak) yüklenmelidir.26 React.lazy ve Suspense kullanımı, bu dinamik yüklemeyi yönetmek için standarttır.27
________________


5. Backend Altyapısı ve Veri Güvenliği: Supabase Mimarisi


Move2Germany, "Backend-as-a-Service" (BaaS) çözümü olarak Supabase'i kullanmaktadır. Bu tercih, kimlik doğrulama, veritabanı (PostgreSQL) ve sunucusuz fonksiyonların (Edge Functions) entegre bir şekilde yönetilmesini sağlar. Ancak, pasaport bilgileri ve iş sözleşmeleri gibi hassas verilerin işlenmesi nedeniyle, güvenlik mimarisi en üst düzeyde tutulmalıdır.


5.1 Veritabanı Şeması ve İlişkisel Tasarım


Konfigürasyon güdümlü yapıyı desteklemek için veritabanı şeması hem yapılandırılmış (SQL) hem de esnek (JSONB) veri tiplerini hibrit olarak kullanmalıdır.
Temel Tablo Yapısı:
   * profiles: Kullanıcı kimlik bilgilerini tutar (auth.users ile ilişkilidir).
   * Kolonlar: id (UUID), email, nationality, target_profession (ENUM: 'IT', 'Health', 'Student'), created_at.
   * flows: Göç yollarının tanımlarını içerir.
   * Kolonlar: id (Text, PK), schema (JSONB - tüm kurallar burada), valid_from, valid_until.
   * user_applications: Kullanıcının başlattığı süreçler.
   * Kolonlar: id (UUID), user_id (FK), flow_id (FK), current_step, status (Pending, Approved).
   * form_data (JSONB): Kullanıcının girdiği veriler (maaş, üniversite adı vb.) şemasız olarak burada tutulur, böylece her yeni alan için tablo değiştirmeye gerek kalmaz.


5.2 Satır Düzeyi Güvenlik (Row Level Security - RLS) Derinlemesine Analiz


Çok kiracılı (multi-tenant) bir mimaride, veri izolasyonu hayati önem taşır. Geleneksel "uygulama seviyesi" filtreleme (örneğin WHERE user_id =...) güvenlik açısından yeterli değildir; zira bir geliştirici hatası tüm veritabanını dışarıya açabilir. Supabase üzerindeki PostgreSQL RLS özelliği, güvenliği veritabanı çekirdeğine taşır.28


5.2.1 RLS Politikaları


Her tablo için katı politikalar tanımlanmalıdır. Örneğin, user_applications tablosu için:


SQL




-- Kullanıcılar sadece kendi başvurularını görebilir
CREATE POLICY "Users can view own applications"
ON user_applications
FOR SELECT
TO authenticated
USING (auth.uid() = user_id);

-- Kullanıcılar sadece kendi adlarına başvuru oluşturabilir
CREATE POLICY "Users can create own applications"
ON user_applications
FOR INSERT
TO authenticated
WITH CHECK (auth.uid() = user_id);

Bu politikalar, veritabanı motoru tarafından her sorguda çalıştırılır. Bir saldırgan SQL enjeksiyonu ile SELECT * FROM user_applications sorgusu çalıştırsa bile, veritabanı ona sadece kendi user_id'sine (JWT token içindeki sub claim) ait satırları döndürür.29


5.2.2 Depolama (Storage) Güvenliği


Kullanıcıların yüklediği belgeler (PDF, JPEG) için de benzer RLS kuralları geçerlidir. Supabase Storage, storage.objects tablosu üzerinden yönetilir. Bir kullanıcının, başka bir kullanıcının pasaport fotokopisine erişmesini engellemek için şu politika uygulanmalıdır:


SQL




CREATE POLICY "Users can manage own files"
ON storage.objects
FOR ALL
TO authenticated
USING (bucket_id = 'documents' AND owner_id = auth.uid());

Bu, dosya erişim kontrolünü uygulama kodundan çıkarıp altyapı seviyesine indirerek insan hatası riskini minimize eder.30


5.3 Görünümler (Views) ve security_invoker


Raporlama panoları için karmaşık SQL sorguları yerine Veritabanı Görünümleri (Views) kullanılacaktır. Ancak PostgreSQL'de görünümler varsayılan olarak security_definer (oluşturanın yetkileriyle) çalışır, bu da RLS'i baypas edebilir. Güvenlik açığını önlemek için, PostgreSQL 15 ve üzeri sürümlerde desteklenen security_invoker = true parametresi kullanılmalıdır. Bu, görünümü çağıran kullanıcının (yani son kullanıcının) yetkilerinin ve RLS kurallarının geçerli olmasını sağlar.29
________________


6. Yapay Zeka Entegrasyonu, Edge Functions ve Performans


Uygulamanın "7/24 Danışman" özelliğini sağlamak için OpenAI GPT-4o modelleri kullanılacaktır. Ancak, doğrudan OpenAI API'sine bağlanmak güvenlik ve maliyet riskleri taşır. Bu nedenle, tüm yapay zeka trafiği Supabase Edge Functions (Deno) üzerinden yönetilecektir.


6.1 Yapay Zeka Mimarisi ve RAG (Retrieval-Augmented Generation)


Yasal konularda yapay zekanın "halüsinasyon" görmesi (olmayan bir yasayı uydurması) kabul edilemez. Bu riski yönetmek için RAG mimarisi kullanılacaktır.
   1. Vektör Veritabanı: Almanya İkamet Yasası (AufenthG), Vize El Kitabı ve BAMF yönergeleri, metin parçalarına ayrılıp vektörlere (pgvector kullanılarak) dönüştürülür ve Supabase'de saklanır.
   2. Sorgu İşleme: Kullanıcı bir soru sorduğunda (örn: "Ailem yanıma gelebilir mi?"), bu soru önce vektöre çevrilir ve veritabanında en alakalı yasal metinler bulunur.
   3. Bağlam (Context) Enjeksiyonu: LLM'e şu komut (prompt) gönderilir: "Sen bir göçmenlik asistanısın. Sadece aşağıda verilen yasal metinlere dayanarak cevap ver. Bilmiyorsan, bilmediğini söyle. Asla yasal tavsiye verme.".32


6.2 Streaming (Akış) Yanıtlar ve Kullanıcı Deneyimi


Yapay zeka modellerinin yanıt üretmesi saniyeler sürebilir. Kullanıcıyı bekletmemek için yanıtlar "Stream" edilmelidir. Supabase Edge Functions, TextDecoderStream kullanarak OpenAI'den gelen yanıt parçacıklarını (chunks) anlık olarak istemciye (React) iletir.34 Bu sayede kullanıcı, yanıtın tamamlanmasını beklemeden okumaya başlayabilir, bu da algılanan performansı artırır.


6.3 Rate Limiting (Hız Sınırlama) ve Maliyet Kontrolü


Yapay zeka API'leri maliyetlidir. Kötü niyetli kullanımı veya aşırı yüklenmeyi önlemek için Edge Functions seviyesinde hız sınırlama uygulanmalıdır.
   * Redis veya Token Bucket: Kullanıcı başına günlük istek limiti (örneğin 50 istek) belirlenir.
   * Hata Yönetimi: Eğer limit aşılırsa, fonksiyon EF047: Function API Rate Limit Exceeded hatası döndürür ve arayüz kullanıcıya "Günlük limitinize ulaştınız, yarın devam edebilirsiniz" mesajını gösterir.36 Bu mantık, veritabanına gereksiz yük binmesini engellemek için anon (giriş yapmamış) kullanıcıları daha sıkı filtrelemelidir.37


6.4 Yasal Sorumluluk Reddi (Disclaimer)


Yasal teknolojilerde (LegalTech), kullanıcının yapay zeka ile konuştuğunun farkında olması yasal bir zorunluluktur (AB Yapay Zeka Yasası). Sohbet penceresinin en üstünde ve ilk mesajda şu metin sabitlenmelidir: "Ben bir yapay zeka asistanıyım. Hata yapabilirim. Verdiğim bilgiler yasal tavsiye niteliğinde değildir, lütfen kritik kararlar için bir avukata danışın.".38
________________


7. Konut Piyasası Agregasyon Motoru ve Dolandırıcılık Tespiti


Konut bulma süreci, göçmenler için en büyük darboğazdır.13 Move2Germany, kendi ilanlarını oluşturmak yerine, mevcut platformları (WG-Gesucht, ImmoScout24) analiz eden ve filtreleyen bir "Agregatör" (Toplayıcı) olarak çalışacaktır.


7.1 Arama URL Mühendisliği ve Entegrasyon




7.1.1 WG-Gesucht Entegrasyonu


WG-Gesucht, özellikle öğrenciler ve paylaşımlı ev arayanlar için birincil kaynaktır. Resmi bir API'si olmasa da, URL yapısı son derece tutarlıdır. Uygulama, kullanıcının tercihlerini (Şehir, Maksimum Kira, Oda Sayısı) alarak dinamik URL'ler oluşturacaktır.
   * URL Deseni: https://www.wg-gesucht.de/wg-zimmer-und-1-zimmer-wohnungen-und-wohnungen-und-haeuser-in-{city}.{city_id}.0+1+2+3.1.{page}.html.40
   * Filtreleme: Kullanıcının girdiği filtreler (örn: min 15m², max 600€), URL parametrelerine (&min_size=15&max_rent=600) dönüştürülür. Bu sayede kullanıcı uygulamadan "Ara" butonuna bastığında, doğrudan filtrelenmiş sonuç sayfasına yönlendirilir, manuel filtreleme zahmetinden kurtulur.41


7.1.2 ImmoScout24 ve Coğrafi Kodlama (Geocoding)


ImmoScout24 entegrasyonu daha tekniktir çünkü şehir isimleri yerine özel "GeoCode" ID'leri kullanır (örneğin Berlin Mitte için 1276003001014). Uygulama, arka planda Şehir İsimleri -> GeoCode ID eşleşmesini tutan bir haritalama tablosu kullanmalıdır.42 Ayrıca, merkezdeki yüksek kiralar nedeniyle banliyöleri de kapsamak için "Yarıçap Araması" (/radius?geocoordinates=lat;long;radius) parametreleri kullanılarak kullanıcılara daha uygun fiyatlı alternatifler sunulabilir.42


7.2 Dolandırıcılık Tespit Algoritmaları (Scam Detection Layer)


Yabancılar, konut dolandırıcılığına karşı en savunmasız gruptur. Uygulama, "İlan Analizörü" özelliği ile kullanıcıları korumalıdır. Kullanıcı şüpheli bir ilanın metnini veya linkini yapıştırdığında, sistem şu sinyalleri tarar:


Dolandırıcılık Tipi
	Belirti / Anahtar Kelime
	Aksiyon
	Uzaktaki Ev Sahibi
	"I am currently in UK/Spain", "Keys will be mailed", "Missionary work" 43
	Kırmızı Alarm: "Bu klasik bir dolandırıcılık senaryosudur. Asla para göndermeyin."
	Kimlik Hırsızlığı
	"Send passport copy before viewing", "Schufa score needed immediately" 45
	Sarı Alarm: "Görmediğiniz ev için kimlik belgesi paylaşmayın."
	Airbnb Tuzağı
	Tersine Görsel Arama sonucu evin Airbnb veya lüks emlak sitelerinde çıkması 46
	Kırmızı Alarm: "Bu görseller başka bir siteden çalınmış olabilir."
	Ödeme Yöntemi
	"Western Union", "MoneyGram", "Cash in envelope", Yabancı IBAN (DE ile başlamayan) 43
	Kritik Uyarı: "Bu ödeme yöntemleri geri alınamaz. İşlemi durdurun."
	Bu analiz katmanı, kullanıcının dolandırılma riskini minimize eder ve platforma olan güveni artırır.
________________


8. Uyumluluk, GDPR ve Operasyonel Metrikler




8.1 Kişisel Verilerin Korunması (PII Redaction)


Almanya, veri gizliliği konusunda dünyanın en katı ülkesidir. Uygulama "Tasarım ile Gizlilik" (Privacy by Design) ilkesine göre inşa edilmelidir. Yapay zeka modellerine veya analitik araçlarına veri gönderilmeden önce, Kişisel Tanımlanabilir Bilgiler (PII) temizlenmelidir.
Bunun için özel Regex (Düzenli İfade) kalıpları kullanılacaktır:
   * Alman Pasaportu: [CFGHJK][0-9]{8} veya [0-9]{9} desenine uyan veriler.47
   * İşlem: Bir "Middleware" (Ara Katman) fonksiyonu, veri paketini tarar, bu desenleri bulur ve `` (GİZLENDİ) etiketiyle değiştirir.49 Bu işlem, yapay zekanın yanlışlıkla kullanıcının pasaport numarasını öğrenmesini veya loglara kaydetmesini engeller.


8.2 Denetim Kayıtları (Audit Logging)


GDPR kapsamında kullanıcıların "Veriye Erişim Hakkı" vardır. Ayrıca güvenlik ihlallerini izlemek için kapsamlı bir loglama şarttır. Supabase üzerindeki pgaudit eklentisi veya özel bir audit_logs tablosu kullanılarak;
   * Kim: İşlemi yapan kullanıcı ID'si.
   * Ne: Hangi tablo, hangi satır.
   * Ne Zaman: Zaman damgası (Timestamp).
   * Değişim: Eski değer / Yeni değer.
bilgileri saklanmalıdır.50 Kullanıcı talep ettiğinde, "Veri Geçmişim" raporu bu tablodan otomatik oluşturulabilir.


8.3 Başarı Metrikleri (KPI'lar)


Uygulamanın başarısı, sadece indirilme sayısıyla değil, kullanıcının hayatına kattığı gerçek değerle ölçülmelidir.51


Metrik
	Tanım
	Hedef
	Neden Önemli?
	Time-to-Value (TTV)
	Uygulama kurulumundan "Vize Onayı" veya "Ev Tutuldu" aşamasına geçen süre.
	< 90 Gün
	Sürecin gerçekten hızlanıp hızlanmadığını ölçer.52
	Scam Avoidance Rate
	Dolandırıcılık uyarısı aldıktan sonra etkileşimi kesen kullanıcı oranı.
	> %80
	Güvenlik katmanının etkinliğini gösterir.
	Tutundurma (Retention)
	İlk 3 ay boyunca uygulamaya geri dönen kullanıcı oranı.
	> %40
	Göç uzun soluklu bir süreçtir; düşük oran kullanıcının Excel'e geri döndüğünü gösterir.54
	Maliyet Tasarrufu
	Önerilen sigorta/bloke hesap sağlayıcıları ile kullanıcının cebinde kalan ortalama tutar.
	> 200€
	Kullanıcıya sunulan somut finansal faydadır.55
	________________


9. Sonuç ve Yol Haritası


"Move2Germany" projesi, salt bir yazılım geliştirme işi değil, karmaşık bir sosyo-yasal problemin teknoloji ile çözülmesi girişimidir. 2025 Mavi Kart reformları ve konut krizi gibi dinamik değişkenler, statik bir uygulama yerine konfigürasyon güdümlü, yapay zeka destekli ve güvenlik odaklı bir mimariyi zorunlu kılmaktadır.
Önerilen React 18 ve Supabase mimarisi, yasal değişikliklere karşı esneklik sağlarken, Edge Functions ve RLS kullanımı hem performansı hem de veri güvenliğini garanti altına almaktadır. Ancak hepsinden önemlisi, "Sakin Tasarım" ve "Dolandırıcılık Kalkanı" gibi özellikler, uygulamanın sadece bürokratik bir araç değil, kullanıcının bu zorlu yolculukta güvenebileceği bir dayanak noktası olmasını sağlayacaktır. Bu rapor, projenin geliştirme aşamasına geçilmesi için gerekli teknik ve stratejik temeli oluşturmaktadır.
Works cited
      1. EU Blue Card - Make it in Germany, accessed November 24, 2025, https://www.make-it-in-germany.com/en/visa-residence/types/eu-blue-card
      2. Apply online for a Blue Card (EU) visa - Consular Services Portal, accessed November 24, 2025, https://digital.diplo.de/Blaue-Karte
      3. Germany EU Blue Card 2025: New Salary Limits - Jobbatical, accessed November 24, 2025, https://www.jobbatical.com/blog/germany-latest-eu-blue-card-salaries-updated
      4. The fast-track procedure for skilled workers - Make it in Germany, accessed November 24, 2025, https://www.make-it-in-germany.com/en/looking-for-foreign-professionals/entering/the-fast-track-procedure-for-skilled-workers
      5. Skilled labour immigration using the fast-track procedure for skilled workers – how does it work? - Make it in Germany, accessed November 24, 2025, https://www.make-it-in-germany.com/en/skilled-labour-immigration-using-the-fast-track-procedure-for-skilled-workers-how-does-it-work
      6. Fast-Track Procedure for Skilled Workers - ZAB, accessed November 24, 2025, https://zab.kmk.org/en/statement-comparability/fast-track-procedure
      7. Blocked Account Germany For American Students - My German University, accessed November 24, 2025, https://www.mygermanuniversity.com/blocked-account-germany
      8. Germany Blocked Account for Visa Applicants: Requirements, Process, and Providers, accessed November 24, 2025, https://www.germany-visa.org/banking-germany/blocked-account/
      9. Funding your German Blocked Account in 2025: How much money do you need? - Expatrio, accessed November 24, 2025, https://www.expatrio.com/about-germany/blocked-amount-2025
      10. German Blocked Account Amount: A Guide for International Students (2025), accessed November 24, 2025, https://www.mygermanuniversity.com/articles/Germany-Blocked-Account-Amount
      11. Blocked Account in Germany for Visa Application, accessed November 24, 2025, https://www.studying-in-germany.org/germany-blocked-account-foreign-students/
      12. Sentiments of Foreigners Living in Germany: Insights & Trends 2024 - The Berlin Life, accessed November 24, 2025, https://theberlinlife.com/sentiments-of-foreigners-living-in-germany/
      13. Germany Unwrapped: Expats Give the Worst Rating Yet - InterNations, accessed November 24, 2025, https://www.internations.org/expat-insider/2024/germany-40462
      14. Designing an Empathetic and Non-Intrusive User Interface for Stress and Anxiety Management - Zigpoll, accessed November 24, 2025, https://www.zigpoll.com/content/how-would-you-approach-creating-a-user-interface-that-helps-users-manage-stress-and-anxiety-while-ensuring-the-experience-remains-empathetic-and-nonintrusive
      15. Designing Calm: UX Principles for Reducing Users' Anxiety - UXmatters, accessed November 24, 2025, https://www.uxmatters.com/mt/archives/2025/05/designing-calm-ux-principles-for-reducing-users-anxiety.php
      16. Crafting calm in chaos: designing for stress - Softhouse, accessed November 24, 2025, https://www.softhouse.se/en/crafting-calm-in-chaos-designing-for-stress/
      17. Community Code of Conduct and Moderation Guidelines - Forum Resources, accessed November 24, 2025, https://community.omnissa.com/forums/topic/72-community-code-of-conduct-and-moderation-guidelines/
      18. Forum Etiquette & Moderation | Office of Equity and Anti-Racism Engagement Page, accessed November 24, 2025, https://www.oeaengagement.ca/moderation
      19. The Essential Content Moderation Glossary, accessed November 24, 2025, https://moderationapi.com/glossary
      20. Toxic keyword lists and filters in 2025, the definitive guide - Sightengine, accessed November 24, 2025, https://sightengine.com/keyword-lists-for-text-moderation-the-guide
      21. How I'm Building a Config-Driven UI (And Why You Should Try It Too) | by Mithlesh sharma, accessed November 24, 2025, https://medium.com/@this.mithlesh/how-im-building-a-config-driven-ui-and-why-you-should-try-it-too-0cf3e680627e
      22. React Integration - JSON Forms, accessed November 24, 2025, https://jsonforms.io/docs/integrations/react/
      23. React Design Patterns: Generating User-configured UI Using The Visitor Pattern, accessed November 24, 2025, https://www.arahansen.com/react-design-patterns-generating-user-configured-ui-using-the-visitor-pattern/
      24. rjsf-team/react-jsonschema-form: A React component for building Web forms from JSON Schema. - GitHub, accessed November 24, 2025, https://github.com/rjsf-team/react-jsonschema-form
      25. Optimizing Bundle Sizes in React Applications: A Deep Dive into Code Splitting and Lazy Loading - Coditation, accessed November 24, 2025, https://www.coditation.com/blog/optimizing-bundle-sizes-in-react-applications-a-deep-dive-into-code-splitting-and-lazy-loading
      26. Code Splitting in React: How to Implement Efficient Bundling - Mindbowser, accessed November 24, 2025, https://www.mindbowser.com/react-code-splitting-guide/
      27. Code-Splitting - React, accessed November 24, 2025, https://legacy.reactjs.org/docs/code-splitting.html
      28. How to Manage Row-Level Security Policies Effectively in Supabase - Medium, accessed November 24, 2025, https://medium.com/@jay.digitalmarketing09/how-to-manage-row-level-security-policies-effectively-in-supabase-98c9dfbc2c01
      29. Row Level Security | Supabase Docs, accessed November 24, 2025, https://supabase.com/docs/guides/database/postgres/row-level-security
      30. Ownership | Supabase Docs, accessed November 24, 2025, https://supabase.com/docs/guides/storage/security/ownership
      31. Storage Access Control | Supabase Docs, accessed November 24, 2025, https://supabase.com/docs/guides/storage/security/access-control
      32. Building simple & effective prompt-based Guardrails - QED42, accessed November 24, 2025, https://www.qed42.com/insights/building-simple-effective-prompt-based-guardrails
      33. Build safe and responsible generative AI applications with guardrails - AWS, accessed November 24, 2025, https://aws.amazon.com/blogs/machine-learning/build-safe-and-responsible-generative-ai-applications-with-guardrails/
      34. Generating OpenAI GPT3 completions | Supabase Docs, accessed November 24, 2025, https://supabase.com/docs/guides/ai/examples/openai
      35. Streaming Data in Edge Functions - Supabase - YouTube, accessed November 24, 2025, https://www.youtube.com/watch?v=9N66JBRLNYU
      36. Supabase Edge Functions Function API Rate Limit Exceeded, accessed November 24, 2025, https://drdroid.io/stack-diagnosis/supabase-edge-functions-function-api-rate-limit-exceeded
      37. Troubleshooting | RLS Performance and Best Practices - Supabase Docs, accessed November 24, 2025, https://supabase.com/docs/guides/troubleshooting/rls-performance-and-best-practices-Z5Jjwv
      38. AI Content Disclaimers For ChatGPT & More: Templates And Best Practices (2026 Ready), accessed November 24, 2025, https://www.feisworld.com/blog/disclaimer-templates-for-ai-generated-content
      39. AI Chatbot Disclaimer | PubMatic, accessed November 24, 2025, https://pubmatic.com/legal/ai-chatbot-disclaimer/
      40. Scraping German Rental Price Data – Part I: Whole Lotta Captchas | Personal Blog, accessed November 24, 2025, https://layandreas.github.io/personal-blog/posts/scraping-wg-gesucht-part-i/
      41. WG-Gesucht Property Search Scraper - Apify, accessed November 24, 2025, https://apify.com/ecomscrape/wg-gesucht-property-search-scraper
      42. Query Parameters - ImmoScout24 API Developer Portal – The Real Estate APIs, accessed November 24, 2025, https://api.immobilienscout24.de/api-docs/search/query-parameters/
      43. r/germany Guide: Common Rental Problems in Germany - Reddit, accessed November 24, 2025, https://www.reddit.com/r/germany/wiki/living/problems/
      44. Avoiding Rental Scams for Expats in Germany, accessed November 24, 2025, https://liveingermany.de/renting-scams-germany/
      45. 5 ways to avoid getting scammed when looking for a place in Germany - IamExpat.de, accessed November 24, 2025, https://www.iamexpat.de/housing/property-news/5-ways-avoid-getting-scammed-when-looking-place-germany
      46. Common housing scams in Germany - All About Berlin, accessed November 24, 2025, https://allaboutberlin.com/guides/housing-scams
      47. Personal Dataset Sample | Germany Passport Number | Download PII Data Examples, accessed November 24, 2025, https://www.protecto.ai/blog/personal-dataset-sample-german-passport-number-download-pii-data-examples
      48. Germany passport number entity definition - Microsoft Learn, accessed November 24, 2025, https://learn.microsoft.com/en-us/purview/sit-defn-germany-passport-number
      49. Strengthen data security with custom PII detection rulesets - GitLab, accessed November 24, 2025, https://about.gitlab.com/blog/enhance-data-security-with-custom-pii-detection-rulesets/
      50. Postgres Auditing in 150 lines of SQL - Supabase, accessed November 24, 2025, https://supabase.com/blog/postgres-audit
      51. The must-know mobile app KPIs for every vertical - Adjust, accessed November 24, 2025, https://www.adjust.com/blog/the-must-know-mobile-app-kpis/
      52. What is Time-to-Value & How to Improve It + Benchmark Report 2024 - Userpilot, accessed November 24, 2025, https://userpilot.com/blog/time-to-value-benchmark-report-2024/
      53. Time to value: 6 Ways to track, measure, and reduce TTV - Paddle, accessed November 24, 2025, https://www.paddle.com/resources/time-to-value
      54. 8 user onboarding metrics and KPIs you should be measuring - Appcues, accessed November 24, 2025, https://www.appcues.com/blog/user-onboarding-metrics-and-kpis
      55. How to Measure Relocation Program Success with 4 KPIs - UrbanBound, accessed November 24, 2025, https://www.urbanbound.com/blog/how-to-measure-your-relocation-programs-success-4-essential-kpis