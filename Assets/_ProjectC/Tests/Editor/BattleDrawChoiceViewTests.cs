using System.Collections; // 코루틴 테스트 사용
using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 유니티 객체 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 선택 버튼 조회 사용
public sealed class BattleDrawChoiceViewTests // 카드 선택 화면 테스트
{ // 테스트 클래스 시작
    private static CardInstance selectedCard; // 선택된 후보 카드
    private static bool? remainingDiscarded; // 남은 카드 처리 결과
    private static void RecordSelection(CardInstance card) // 선택 결과 기록
    { // 결과 기록 시작
        selectedCard = card; // 선택 카드 저장
    } // 결과 기록 종료
    private static void RecordRemainingChoice(bool discard) // 남은 카드 처리 결과 기록
    { // 결과 기록 시작
        remainingDiscarded = discard; // 남은 카드 처리 결과 저장
    } // 결과 기록 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator ChoiceView_PaginatesAndSelectsCardsBeyondFirstPage() // 여러 후보 페이지와 선택 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        GameObject canvas = new GameObject("ChoiceTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 캐릭터 목록 조회
        Assert.IsNotNull(roster); // 캐릭터 목록 존재 확인
        CharacterData owner = roster.Characters[0]; // 실제 카드 소유자 조회
        Assert.IsNotNull(owner); // 카드 소유자 존재 확인
        CardData source = Resources.Load<CardData>("StarterCards/Attack"); // 실제 카드 원본 조회
        Assert.IsNotNull(source); // 카드 원본 존재 확인
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(owner); // 아군 유닛 생성
        CardInstance[] cards = { CardInstance.Create(source, unit, 0, 0), CardInstance.Create(source, unit, 1, 0), CardInstance.Create(source, unit, 2, 0) }; // 서로 다른 후보 세 장 생성
        selectedCard = null; // 선택 결과 초기화
        BattleDrawChoiceView view = BattleDrawChoiceView.Show(canvas.transform, cards, RecordSelection); // 후보 선택 화면 생성
        Transform nextObject = view.transform.Find("NextPage"); // 다음 페이지 버튼 조회
        Assert.IsNotNull(nextObject); // 다음 페이지 버튼 존재 확인
        Transform previousObject = view.transform.Find("PreviousPage"); // 이전 페이지 버튼 조회
        Assert.IsNotNull(previousObject); // 이전 페이지 버튼 존재 확인
        Assert.IsFalse(previousObject.GetComponent<Button>().interactable); // 첫 페이지 이전 버튼 차단 확인
        Assert.IsTrue(nextObject.GetComponent<Button>().interactable); // 첫 페이지 다음 버튼 허용 확인
        nextObject.GetComponent<Button>().onClick.Invoke(); // 다음 페이지 이동
        Assert.IsTrue(previousObject.GetComponent<Button>().interactable); // 마지막 페이지 이전 버튼 허용 확인
        Assert.IsFalse(nextObject.GetComponent<Button>().interactable); // 마지막 페이지 다음 버튼 차단 확인
        Transform thirdChoice = view.transform.Find("Choice_2"); // 세 번째 후보 버튼 조회
        Assert.IsNotNull(thirdChoice); // 세 번째 후보 표시 확인
        thirdChoice.GetComponent<Button>().onClick.Invoke(); // 세 번째 후보 선택
        Assert.AreSame(cards[2], selectedCard); // 올바른 후보 선택 확인
        Object.Destroy(canvas); // 테스트 캔버스 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
    [UnityTest] // 코루틴 테스트 지정
    public IEnumerator RemainingChoice_AllowsKeepingCardOnDeck() // 남은 카드 덱 유지 선택 확인
    { // 테스트 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        GameObject canvas = new GameObject("RemainingTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        CharacterData owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 카드 소유자 조회
        CardData source = Resources.Load<CardData>("StarterCards/Attack"); // 카드 원본 조회
        CardInstance card = CardInstance.Create(source, BattleUnitRuntime.CreateAlly(owner), 0, 0); // 선택 카드 생성
        remainingDiscarded = null; // 선택 결과 초기화
        BattleRemainingCardChoiceView view = BattleRemainingCardChoiceView.Show(canvas.transform, card, RecordRemainingChoice); // 남은 카드 선택 화면 생성
        view.transform.Find("KeepRemaining").GetComponent<Button>().onClick.Invoke(); // 덱 맨 위 유지 선택
        Assert.AreEqual(false, remainingDiscarded); // 버리지 않음 선택 결과 확인
        Object.Destroy(canvas); // 테스트 캔버스 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 테스트 종료
} // 테스트 클래스 종료
