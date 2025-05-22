using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Tutorial : MonoBehaviour
{
    string arabicVersion= "مرحبًا بك في Survive the ColdwarSPECIAL MARK\r\nأساسيات اللعبة سهلة جدًا ويمكنك إتقانها خلال أقل من خمس دقائقSPECIAL MARK\r\nلكن، هذا لا يعني أن اللعبة سهلة!!!SPECIAL MARK\r\nالتاريخ هنا هو: 1947، وهو العام الذي بدأت فيه الحرب الباردةSPECIAL MARK\r\nانتهت في عام 1991 وكل ما عليك فعله في هذه اللعبة هو أن تنجو حتى ذلك العام.SPECIAL MARK\r\nأنت تتحكم في كلا طرفي الحرب.SPECIAL MARK\r\nتمثل هذه القيم القوة والمال ورضا الشعب لكل جانب.SPECIAL MARK\r\nعندما يكون التاريخ باللون الأخضر، فهذا يعني أن الوقت يتحرك، وعندما يكون أحمر فهذا يعني أن اللعبة متوقفة. انتظر حتى نهاية الشهر.SPECIAL MARK\r\nراقب تغيّرات القيم لدى كلا الطرفين، خصوصًا القوة (رمز البرق)، وكذلك تقدّم المساحات لكل دولة.SPECIAL MARK\r\nلنُسرّع الأمور قليلًا، اضغط مع الاستمرار على زر التسريع، أو اضغط عليه عدة مرات بسرعة.SPECIAL MARK\r\nالحمر يزدادون قوة، إذا استمر الحال هكذا ستخسر.SPECIAL MARK\r\nهذا الشريط يُظهر نسبة الخطر ومدى قرب اندلاع حرب نووية. يزداد هذا الرقم كلما زاد الفارق في القوة بين الدولتين، وإذا وصل مستوى الخطر إلى 100 ستخسر.SPECIAL MARK\r\nللحفاظ على توازن الحرب الباردة، اضغط على النجمة الزرقاء لبدء حرب بالوكالة من جانب الزُرق \"رمز الدرع\".SPECIAL MARK\r\nابدأ 3 حروب بالوكالة للتقدّم.SPECIAL MARK\r\nاضغط في أي مكان على الخريطة أو على النجمة الزرقاء لإخفاء قائمة الأوامر.SPECIAL MARK\r\nانتظر ثلاثة أشهر الآن أو اضغط على التسريع لبدء الحروب.SPECIAL MARK\r\nعند النقر على هذه النقاط الخضراء الصغيرة، تُعرض معلومات عن قوة الدولة، أموالها، ورضا شعبها.SPECIAL MARK\r\nيبدو أن ثلاث حروب لا تكفي، اضغط على النجمة الزرقاء مرة أخرى لاتخاذ إجراء آخر.SPECIAL MARK\r\nاضغط على رمز \"المفتاح\" لبدء بحث علمي.SPECIAL MARK\r\nلا يمكنك بدء بحث حاليًا لأن الزُرق لا يملكون ما يكفي من المال.SPECIAL MARK\r\nاضغط على رمز \"الترس\" لبناء مصنع، بناء المصانع يزيد من الدخل الشهري للدولة.SPECIAL MARK\r\nقم ببناء مصنعين.SPECIAL MARK\r\nأوه لا، حان الوقت لاتخاذ إجراء في جانب الحمر. اضغط على النجمة الحمراء.SPECIAL MARK\r\nاضغط على رمز \"التاج\" لتغيير الرئيس.SPECIAL MARK\r\nتعرض هذه النافذة الرؤساء المتاحين للاختيار، والأرقام بجانب كل اسم تمثّل الأرباح الشهرية الناتجة عن اختيار كل منهم.SPECIAL MARK\r\nفي الوقت الحالي، نحتاج لإضعاف الحمر لأنهم أصبحوا أقوياء جدًا، ولهذا السبب اختر \"Yann Golubev\" لأنه يسبب نتائج سلبية كبيرة.SPECIAL MARK\r\nتغيير الرؤساء عامل مهم في الحرب، لكن لا يمكنك تغييره دائمًا، يجب أن يمر عام واحد على الأقل لتتمكن من تغييره.SPECIAL MARK\r\nالانتخابات الدورية تحدث كل 4 سنوات للزُرق، وكل 7 سنوات للحمر.SPECIAL MARK\r\nاضغط على معلومات أي دولة لمعرفة كل من السنوات المتبقية والسنوات السابقة للرئيس.SPECIAL MARK\r\nالرقم الأخضر يمثل الأشهر الماضية في الرئاسة. الأرقام البيضاء تُمثل الأشهر المتبقية للرئيس. السهم البرتقالي يرمز للإجراءات الجارية حاليًا للدولة، أما الرقم الصغير المُشار إليه بالسهم الأحمر فيمثّل عدد الأشهر المتبقية لانتهاء هذا الإجراء.SPECIAL MARK\r\nسيسير كل شيء على ما يرام الآن، لكن بعد فترة، سيصل الحمر إلى الصفر = خسارة اللعبة.SPECIAL MARK\r\nاضغط على النجمة الحمراء وأرسل جاسوسًا، رمز \"علامة الاستفهام\".SPECIAL MARK\r\nيكسب الجاسوس قدرًا كبيرًا من القوة في كل دور، لذلك عليك أيضًا اتخاذ إجراء في جانب الزُرق للحفاظ على التوازن.SPECIAL MARK\r\nاضغط على النجمة الزرقاء وابدأ بحثًا علميًا.SPECIAL MARK\r\nالآن انتظر لبضع سنوات لرؤية نتائج أفعالك (سرّع اللعبة).SPECIAL MARK\r\nأصبح الحمر ضعفاء جدًا الآن، مما يترك لهم خيارًا واحدًا فقط، الاغتيال!!SPECIAL MARK\r\nاضغط على النجمة الحمراء لإرسال قاتل مأجور، انتبه فهذه الخطوة خطيرة جدًا ونتيجتها غير مضمونة، اترك هذا الخيار للنهاية دائمًا!SPECIAL MARK\r\nعظيم، الآن انتظر قليلًاSPECIAL MARK\r\nحسنًا، هذا هو كل ما في اللعبة تقريبًا، الآن ستعود إلى اللعبة الرئيسية دون قيود لترى إن كنت قادرًا على النجاة في الحرب الباردة!SPECIAL MARK\r\nأمر أخير. يمكنك دائمًا كتم الموسيقى من خلال النقر على رمز \"اللحن\"، وكتم المؤثرات الصوتية من خلال النقر على رمز \"تشغيل\"، وإيقاف اللعبة مؤقتًا من خلال زر الإيقاف.SPECIAL MARK";
    public static Tutorial instance;
    public static int Level=0;
    public string[] Texts;
    public string[] arabicTexts;
    public GameObject TutWin, country_Info;
    public GameObject[] Date, Blue, UI, Nuke,speed,War,Greens,Research,Factory,ComCenter,Spy,Assassin,Arrows,CantRes,Crown;
    GameObject[] MapSector;
    bool DateLock, BlueLock, UILock, NukeLock,FactoryLock,ElectionLock, speedLock, YannLock,NextLock, WarLock;
    CountryManager countryManager;
    public static bool FirstMonthLock, SecondMonthLock,FirstYear,ThreeWARS,TwoFactories, LongWait, ENDLOCK;
    private void Awake()
    {
        instance = this;
        countryManager = GameObject.FindGameObjectWithTag("Canvas").GetComponent<CountryManager>();
        speedLock=DateLock = BlueLock = UILock = NukeLock = FactoryLock=ElectionLock= YannLock=NextLock= WarLock=false;
        FirstMonthLock = SecondMonthLock= FirstYear= ThreeWARS= TwoFactories=LongWait= ENDLOCK=true;
        MapSector = GameObject.FindGameObjectsWithTag("MapSector");
        arabicTexts = Translate(arabicVersion);
        tutorialTextFixer = TutWin.transform.GetChild(0).GetComponent<ArabicFixer>();
        tutorialText=tutorialTextFixer.GetComponent<Text>();
    }
    
    private void Start()
    {
        Time.timeScale = 0;
        Level = -1;
        Next(false);
    }
    private string[] Translate(string english)
    {
        english= english.Replace("SPECIAL MARK", "|");
        string[] lines = english.Split('|');
        return lines;
    }
    void Update()
    {
        if (Level == 3&&!DateLock)
            DateLock=ActivitatingReturn(Date);
        if (Level==4)
            DeActivitingArrows(Date, 2);
        if (Level == 6 && !UILock)
            UILock = ActivitatingReturn(UI);
        if (Level == 7&&UILock)
        {
            UILock = false;
            Time.timeScale = 1;
            NextActiviting(false);
        }
        if (Level == 7 && !FirstMonthLock)
            Next(false);
        if (Level == 8 && !SecondMonthLock)
            Next(false);
        if (Level == 9 && !speedLock)
            speedLock= ActivitatingReturn(speed);
        if (Level == 9 && !FirstYear)
        {
            Time.timeScale = 0;
            NextActiviting(true);
            DeActivitingArrows(speed, 1);
            Next(false);
        }
        if (Level == 11 && !NukeLock)
            NukeLock= ActivitatingReturn(Nuke);
        if (Level == 12 && !BlueLock)
        {
            MapSectorInterActable(false);
            DeActivitingArrows(Nuke, 1);
            BlueLock= ActivitatingReturn(Blue);
            NextActiviting(false);
            Time.timeScale = 0.0000000001f;
            Activiting(War);
        }
        if (Level == 12 && countryManager.ActionMenu.activeSelf && !WarLock)
        {
            WarLock = true;
            DeActivitingArrows(Blue, 1);
            InterActable(Blue);
        }
        if (Level == 13 && WarLock)
        {
            DeActivitingArrows(War, 1);
            WarLock = false;
        }
        if (Level == 13 && countryManager.Capitalism.WarLevel == 3)
        {
            Next(false);
            InterActable(War);
            MapSectorInterActable(true);
        }

        if (Level == 14 && !countryManager.ActionMenu.activeSelf)
        {
            MapSectorInterActable(false);
            Next(false);
            InterActable(Blue);
            Time.timeScale = 1;
        }
        if (Level == 15 && countryManager.Capitalism.WarNumber == 3)
        {
            Activiting(Greens);
            Next(false);
        }
        if (Level == 16 && !ThreeWARS)
        {
            Next(false);
            Time.timeScale = 0.000001f;
            InterActable(speed);
        }
        if (Level == 17)
        {
            country_Info.SetActive(false);
            InterActableGreen(false);
            Activiting(Blue);
            InterActable(Blue, true);
            DeActivitingArrows(Greens, 2);
        }

        if (Level == 17 && countryManager.ActionMenu.activeSelf)
        {
            Next(false);
            DeActivitingArrows(Blue, 1);
            InterActable(War);
        }
        if (Level == 18)
            Activiting(Research);

        if (Level == 18 && GameManager.nameOfAction == "Research")
        {
            Next(false);
            GameManager.Info.transform.GetChild(21).GetComponent<Button>().interactable = false;
            GameManager.Info.transform.GetChild(24).GetComponent<Button>().interactable = true;
        }

        if (Level == 19 && !WarLock)
        {
            WarLock = true;
            NextActiviting(true);
            DeActivitingArrows(Research, 1);
            InterActable(Research);
            Activiting(CantRes);
        }

        if (Level == 20 && !FactoryLock)
        {
            GameManager.Info.transform.GetChild(21).GetComponent<Button>().interactable = true;
            GameManager.Info.transform.GetChild(24).GetComponent<Button>().interactable = false;
            DeActivitingArrows(CantRes, 0);
            FactoryLock= ActivitatingReturn(Factory);
            GameManager.Singleton.ShowInfo(false);
            NextActiviting(false);
        }
        if (Level == 21 && countryManager.Capitalism.FactoryLevel < 3)
        {
            Time.timeScale = 0.000001f;
            InterActable(speed);
        }
        if (Level == 21 && countryManager.Capitalism.FactoryLevel == 3 && FactoryLock)
        {
            FactoryLock = false;
            countryManager.ActionMenu.SetActive(false);
            GameManager.Singleton.ShowInfo(false);
            InterActable(Blue);
            InterActable(Factory);
            DeActivitingArrows(Factory, 1);
            Time.timeScale = 1;
            InterActable(speed, true);
        }
        if (Level == 21 && countryManager.Capitalism.FactoriesNumber == 2)
        {
            Time.timeScale = 0.00000001f;
            countryManager.ActionMenu.SetActive(false);
            GameManager.Singleton.ShowInfo(false);
            speed[0].GetComponent<Button>().interactable = false;
            Next(false);
            Activiting(ComCenter);
            InterActable(ComCenter, true);
        }
        if (Level == 22 && countryManager.ActionMenu.activeSelf)
        {
            Next(false);
            DeActivitingArrows(ComCenter, 1);
            Activiting(Crown);
        }
        if (Level >= 22 && Level <= 33)
        {
            InterActable(speed);
            Time.timeScale = 0.01f;
        }
        if (Level == 23 && countryManager.ELectionWindow.activeSelf && !ElectionLock)
        {
            DeActivitingArrows(Crown, 1);
            InterActable(speed, true);
            ElectionLock = true;
            DeActivitingArrows(Crown, 1);
            InterActable(Crown);
            InterActable(ComCenter);
            Next(false);
            NextActiviting(true);
            foreach (Button item in countryManager.ELectionWindow.GetComponentsInChildren<Button>())
                item.interactable = false;
        }
        if (Level == 25 && !YannLock)
        {
            InterActable(speed);
            NextActiviting(false);
            YannLock = true;
            countryManager.ELectionWindow.transform.GetChild(0).GetChild(7).GetComponent<Button>().interactable = true;
        }
        if (Level == 25 && !countryManager.ELectionWindow.activeSelf)
        {
            speed[0].GetComponent<Button>().interactable = false;
            Time.timeScale = 0;
            Next(false);
            NextActiviting(true);
        }
        if (Level == 28 && !NextLock)
        {
            speed[0].GetComponent<Button>().interactable = false;
            InterActableGreen(true);
            NextActiviting(false);
            NextLock = true;
        }

        if (Level == 28 && CountryManager.CountryInfo.activeSelf && NextLock)
        {
            DeActivitingArrows(Greens, 2);
            InterActable(speed);
            NextLock = false;
            Next(false);
            NextActiviting(true);
            Activiting(Arrows);
        }
        if (Level == 29 && !NextLock && CountryManager.CountryInfo.activeSelf)
        {
            InterActableGreen(false);
            MapSectorInterActable(false);
        }
        if (Level == 30)
        {
            CountryManager.CountryInfo.SetActive(false);
            DeActivitingArrows(Arrows, 0);
            Activiting(Spy);
        }
        if (Level == 31 && !NextLock)
        {
            InterActable(ComCenter, true);
            NextLock = true;
            NextActiviting(false);
        }
        if (Level == 32 && NextLock)
        {
            speed[0].GetComponent<Button>().interactable = false;
            DeActivitingArrows(Spy, 1);
            InterActable(Spy);
            InterActable(ComCenter);
            NextLock = false;
        }
        if(Level==32&&countryManager.Communism.Actions[0] != null)
        {
            NextActiviting(true);
        }

        if (Level == 33 && !NextLock)
        {
            NextLock = true;
            InterActable(Blue, true);
            InterActable(Research, true);
            NextActiviting(false);
            countryManager.Capitalism.Money = 10000500000;
            countryManager.ActionMenu.SetActive(false);
            GameManager.Singleton.ShowInfo(false);
            countryManager.Capitalism.PresedncyPeriod = 100;
            if (countryManager.Communism.Actions[0] == null)
                countryManager.Communism.Actions[0] = new ActionFunction("Spy","زرع جاسوس", "Spy is in place","الجاسوس في موقعه", 500, 0, 12, 0, true);
            countryManager.Communism.Money += 50000000000;
            countryManager.Communism.FactoriesTotalPower = -155;
        }
        if (Level == 33 && NextLock && countryManager.Capitalism.Actions[0] != null)
        {
            if (countryManager.Capitalism.Actions[0].name != "Research")
                countryManager.Capitalism.Actions[0] = new ActionFunction("Research", "تمويل بحث علمي", "Research Completed", "تم إنهاء البحث", 3000, 10, 24, 0, false);
            Next(false);
            NextLock = false;
            countryManager.ActionMenu.SetActive(false);
            GameManager.Singleton.ShowInfo(false);
            InterActable(speed, true);
            Time.timeScale = 1;

        }
        if (Level == 34)
        {
            InterActable(Blue);
            InterActable(ComCenter);
            InterActable(Research);
            NextLock = true;
        }
        if (Level == 34 && !LongWait)
        {

            Time.timeScale = 0;
            Next(false);
            Activiting(Assassin);
            NextActiviting(true);
        }
        if (Level == 36 && NextLock)
        {
            InterActable(ComCenter, true);
            countryManager.Communism.PeoplSatsfaction = 75;
            Time.timeScale = 0.00001f;
            NextLock = false;
            NextActiviting(false);
        }
        if (Level == 36 && YannLock && countryManager.Communism.Actions[0] != null)
        {
            countryManager.Communism.Actions[0] = new ActionFunction("Assassin", "عملية اغتيال", "Assassination completed","تمت عملية الاغتيال", 0, 0, 1, 0, false);
            YannLock = false;
            Time.timeScale = 1;
            countryManager.ActionMenu.SetActive(false);
            GameManager.Singleton.ShowInfo(false);
            GameManager.Singleton.WaitAfterAssassinToFinishGame();
            InterActable(Blue);
            InterActable(ComCenter);
            InterActable(speed, true);
            Next(false);
        }
        if (Level == 37 && !ENDLOCK)
        {
            Time.timeScale = 0;
            InterActable(speed);
            Next(false);
            NextActiviting(true);
        }
        if (Level == 40)
        {
            this.gameObject.GetComponent<CountryManager>().Music.Stop(gameObject);
            Time.timeScale = 1;
            SceneManager.LoadScene(2);
            Level = 0;
            speedLock = DateLock = BlueLock = UILock = NukeLock = FactoryLock = ElectionLock = YannLock = NextLock = WarLock = false;
            FirstMonthLock = SecondMonthLock = FirstYear = ThreeWARS = TwoFactories = LongWait = ENDLOCK = true;
        }
        if (Level >= 7 && Level <= 13)
        {
            MapSectorInterActable(false);
        }
    }
    public void Next(bool IsButton)
    {
        print("next");
        if (IsButton)
            this.GetComponent<CountryManager>().ClickSound.Post(gameObject);
        Level++;
        if(LanguageManager.Singlton.GetSelectedLanguag==Language.Arabic)
            TutWin.transform.GetChild(0).GetComponent<ArabicFixer>().fixedText= arabicTexts[Level];
        else
            TutWin.transform.GetChild(0).GetComponent<ArabicFixer>().fixedText = Texts[Level];
    }
    bool ActivitatingReturn(GameObject[] group)
    {
        foreach (GameObject item in group)
            item.SetActive(true);
        return true;   
    }
    void DeActivitingArrows(GameObject[] group,int RealGroupCount)
    {
        for (int i = RealGroupCount; i < group.Length; i++)
        {
            group[i].SetActive(false);
        }
    }

    void Activiting(GameObject[] group)
    {
        foreach (GameObject item in group)
            item.SetActive(true);
    }
    void NextActiviting(bool Active)
    {
        TutWin.transform.GetChild(1).gameObject.SetActive(Active);
    }
    void InterActable(GameObject[]objects)
    {
        objects[0].GetComponent<Button>().interactable = false;
    }
    void InterActableGreen(bool Active)
    {
        Greens[0].GetComponent<Button>().interactable = Active;
        Greens[1].GetComponent<Button>().interactable = Active;
    }
    void InterActable(GameObject[] objects,bool Activite)
    {
        if (Activite)
            objects[0].GetComponent<Button>().interactable = Activite;
    }
    void MapSectorInterActable(bool Active)
    {
        foreach (GameObject item in MapSector)
            item.GetComponent<Button>().interactable = Active;
    }
    ArabicFixer tutorialTextFixer;
    Text tutorialText;
    public void OnLanguageChanged()
    {
        if (LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
        {
            tutorialTextFixer.fixedText = arabicTexts[Level];
            //tutorialText.alignment = TextAnchor.UpperRight;
        }
        else
        {
            tutorialTextFixer.fixedText = Texts[Level];
            //tutorialText.alignment = TextAnchor.UpperLeft;
        }
    }
}
