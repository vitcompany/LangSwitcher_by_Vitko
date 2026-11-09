namespace LangSwitcher;

/// <summary>Частотные слова для обучения языковых моделей и словарной проверки.
/// Расширить словари можно файлами dict_en.txt / dict_ru.txt в папке настроек.</summary>
public static class WordLists
{
    public const string En = """
the be to of and a in that have i it for not on with he as you do at this but his by from they we say her she or
an will my one all would there their what so up out if about who get which go me when make can like time no just him
know take people into year your good some could them see other than then now look only come its over think also back
after use two how our work first well way even new want because any these give day most us is was are were been has
had did does said made went got done being am don't doesn't didn't isn't aren't wasn't weren't can't won't wouldn't
couldn't shouldn't i'm it's that's you're we're they're i've you've we've i'll you'll he's she's let's there's what's
here where why very much more many such through while should may might must own same each few those both between
under again never always often still before since around another really something nothing anything everything someone
anyone everyone nobody thing things man men woman women child children life world hand hands part place case week
company system program question government number night point home water room mother area money story fact month lot
right study book eye eyes job word words business issue side kind head house service friend friends father power hour
game line end member law car city community name president team minute idea kid body information school face others
level office door health person art war history party result change morning reason research girl guy moment air
teacher force education foot boy age policy process music market sense nation plan college interest death experience
effect class control care field development role effort rate heart show leader light voice wife police mind price
report decision son view relationship town road arm difference value building action model season society tax director
position player record paper space ground form event official matter center couple site project activity star table
need court oil situation cost industry figure street image phone data picture practice piece land product doctor wall
patient worker news test movie north south east west love support technology step baby computer type attention film
tree source organization hair window evidence population truth song letter language english russian problem problems
example list group country family student students state states order issue issues task tasks meeting customer price
tell ask seem feel try leave call keep let begin help talk turn start hear play run move live believe hold bring
happen write provide sit stand lose pay meet include continue set learn lead understand watch follow stop create speak
read allow add spend grow open walk win offer remember consider appear buy wait serve die send expect build stay fall
cut reach kill remain suggest raise pass sell require decide pull check fix close save load find found finding gets
getting goes going gone came coming knew known thought thinking looked looking wanted wants used using says told asked
called tried left felt became become becomes shows showed works worked working makes making took taken gave given saw
seen keeps kept started starting needs needed loved likes liked helps helped sent written wrote reading playing
running doing trying talking walking sitting waiting changed changes created moved paid played happened believed
good new first last long great little old big high different small large next early young important bad able best
better sure free true whole real full special easy clear recent certain personal red difficult available likely short
single current wrong private past foreign fine common poor natural similar hot dead central happy serious ready simple
general blue dark various entire close legal cold final main green nice huge popular black white yellow fast slow cool
awesome amazing funny beautiful strong weak late quick open empty
hello hi hey thanks thank please yes yeah yep nope okay ok sorry bye email mail file files folder windows microsoft
google internet online website web user users password login download update install version server client code test
debug build release error errors warning settings setting window click button page link copy paste text keyboard mouse
screen desktop office excel chat message messages send reply today tomorrow yesterday monday tuesday wednesday
thursday friday saturday sunday weekend morning evening night
about above across against along among behind below beside beyond down during except inside near off onto outside per
toward upon within without
however although therefore though perhaps already almost enough quite rather together yet ever else instead maybe
actually probably especially usually simply too soon later once twice
one two three four five six seven eight nine ten hundred thousand million
github git commit push merge branch api app apps java python script html css json sql linux
""";

    public const string Ru = """
и в во не на я что тот быть с со а весь это как она он оно они по но к ко у ты из мы за вы так же от сказать этот
который которая которое которые которого которой которых которым мочь человек о об один одна одно одни еще бы такой
такая такое такие только себя свой своя свое свои своих своим какой какая какое какие каких когда уже для вот кто да
говорить год знать мой моя мое мои моих до или если время рука нет самый самая самое ни стать большой даже другой наш
наша наше наши наших свой ну под где дело есть сам сама само сами раз чтобы два две там чем глаз жизнь первый день тут
ничто ничего потом очень хотеть ли при голова надо без видеть идти теперь тоже стоять друг дом сейчас можно после
слово здесь думать место спросить через лицо тогда ведь хороший каждый новый жить должный смотреть почему потому
сторона просто нога сидеть понять иметь делать вдруг над взять никто сделать дверь перед нужный понимать казаться
работа три ваш ваша ваше ваши уж земля конец несколько час голос город последний пока хорошо давать вода более хотя
всегда второй куда пойти стол ребенок увидеть сила отец женщина машина случай ночь сразу мир совсем остаться вид выйти
дать работать любить старый почти ряд оказаться начало твой твоя твое твои вопрос много война снова ответить между
подумать опять белый деньги значит история нужно бог начать молодой жена дорога
меня мне мной тебя тебе тобой его него ему нему им ним ее нее ей ней их них ими ними нас нам нами вас вам вами себе собой
эта эти этого этой этих этому этим этом том тем той те то та тех
чего чему кого кому кем ком
был была было были будет будут буду будем будешь будете
могу может можешь можем можете могут мог могла могло могли
хочу хочет хочешь хотим хотите хотят хотел хотела хотели хочется
знаю знает знаешь знаем знаете знают знал знала знали
говорю говорит говоришь говорим говорят говорил говорила говорили сказал сказала сказали скажу скажи скажите
делаю делает делаешь делаем делают делал делала делали сделал сделала сделали сделаю сделаем сделай
думаю думает думаешь думал думала думали
вижу видит видишь видел видела видели
иду идет идут шел шла шли пошел пошла пошли пришел пришла пришли ушел ушла
работаю работает работаешь работают работал работала работали
люблю любит любишь любят любил любила
понимаю понимает понимаешь понял поняла поняли понятно
смотрю смотрит смотри посмотреть посмотри посмотрю
пишу пишет писать написать написал написала напиши напишите
читать читаю читает прочитать прочитал
слушать слышать слышу слышишь
ждать жду ждет ждем подожди подождите
помочь помогу помоги помогите помощь
купить куплю купил продать платить звонить позвонить позвоню позвони
отправить отправил отправила отправлю отправь получить получил получила получу
найти нашел нашла нашли искать ищу ищет
открыть открыл открой закрыть закрыл закрой включить выключить проверить проверил проверь проверка
начать начал начала начинать закончить закончил
беру берет брать взял взяла возьми дай давай дам дал дала даст
ем ест пить пью спать сплю ехать еду едет приехать приеду уехать
стал стала стали стану станет
живу живет жил жила
привет здравствуйте здравствуй спасибо пожалуйста извините извини простите прости добрый доброе добрая утро утра
вечер вечера ночи отлично ладно конечно сегодня завтра вчера скоро давно недавно
зачем сколько откуда
человека людей люди людям года лет дня дней раза работы работу работе работой дома доме домой жизни дела делу делом
руки руку руках глаза мира слова месте места лица друга друзья друзей вопросы вопроса города страна страны стране
денег машины компания компании компанию проект проекта проекты задача задачи задачу задач файл файлы файла папка
письмо письма сообщение сообщения телефон телефона компьютер программа программы программу система системы систему
данные данных сайт сайта клиент клиента клиенты документ документы документа встреча встречи встречу неделя недели
неделе месяц месяца минута минуты минут час часа часов цена цены ответ ответа ответы тема темы мама папа брат сестра
дети детей сын дочь семья семьи школа школы улица улице утром вечером днем ночью
новая новое новые нового новой хорошая хорошее хорошие хорошего большая большое большие маленький маленькая
маленькое русский русская русские английский язык языка первая первое последняя последнее другая другое другие
другого нужен нужна нужны должен должна должно должны готов готова готово важный важно нельзя главный главное
правильно неправильно интересно интересный красивый красиво плохо плохой лучший
также именно вообще никогда иногда часто быстро медленно точно действительно наверное возможно примерно вместе туда
сюда далеко близко мало больше меньше лучше хуже поэтому однако зато либо около среди вокруг про разве неужели все
всех всем всего вся всю всеми
ок ага угу ой эх
четыре пять шесть семь восемь девять десять сто тысяча
москва россия яндекс гугл интернет сервер код тест ошибка ошибки ошибку настройки настройка обновление версия пароль
логин скачать установить кнопка ссылка текст экран клавиатура мышь окно чат
""";
}
