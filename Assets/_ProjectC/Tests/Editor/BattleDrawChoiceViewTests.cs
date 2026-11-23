using System.Collections; // 코루틴 테스트 사용
using NUnit.Framework; // 테스트 기능 사용
using TMPro; // 카드 설명과 수치 글자 조회
using UnityEditor; // 독립 카드에 긴 시험 설명 연결
using UnityEngine; // 유니티 객체 사용
using UnityEngine.EventSystems; // 휠과 선택의 실제 UI 이벤트 경로 확인
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
        Assert.IsNull(view.transform.Find("RelicNameplate")); // 일반 카드 효과의 선택창에 유물 명패 미생성 확인
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
    [UnityTest] // 선택창의 패널 배치와 긴 설명 검사 등록
    public IEnumerator ChoiceView_SeparatesArtworkAndDescriptionAndKeepsCardInput() // 패널 외형, 설명 스크롤 및 실제 선택 경로 확인
    { // 후보 설명 스크롤 검사 시작
        yield return new EnterPlayMode(); // 실제 UI 레이아웃과 입력 계약 준비
        GameObject canvas = new GameObject("ChoiceLayoutCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 시험 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 게임 시작의 씬 전환 중 시험 화면 유지
        CardData original = Resources.Load<CardData>("StarterCards/Attack"); // 실제 시작 카드 원본 조회
        CardData expanded = Object.Instantiate(original); // 긴 설명을 붙일 독립 원본 사본 생성

        try // 독립 화면과 시험 카드 정리 보장
        { // 카드 레이아웃 검증 시작
            SerializedObject edit = new SerializedObject(expanded); // 별도 설명을 쓸 카드 사본 접근
            edit.FindProperty("description").stringValue = new string('가', 540); // 본문 영역을 확실히 넘는 시험용 설명
            edit.ApplyModifiedPropertiesWithoutUndo(); // 시험 사본에만 긴 설명 설정
            CharacterData owner = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 후보 카드 소유자 조회
            BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(owner); // 독립 아군 런타임 생성
            CardInstance shortCard = CardInstance.Create(original, unit, 0, 0); // 현재 기본 카드 설명 후보 생성
            CardInstance longCard = CardInstance.Create(expanded, unit, 1, 0); // 긴 설명을 가진 후보 생성
            CardInstance chosen = null; // 선택으로 전달된 후보 기록
            BattleDrawChoiceView view = BattleDrawChoiceView.Show( // 실제 명패가 있는 선택창 열기
                canvas.transform, // 독립 시험 캔버스
                new[] { shortCard, longCard }, // 길이가 다른 원래 두 후보
                card => chosen = card, // 선택된 카드 확인 동작
                titleText: "가져올 카드 선택", // 중앙 패널 안 제목
                cancelled: () => Assert.Fail("카드 선택 대신 취소 호출"), // 선택 중 잘못된 취소 검사
                relicName: "서고 먼지갈피"); // 중앙 패널 위 이름
            yield return null; // TMP 및 스크롤 적용 프레임 대기
            Canvas.ForceUpdateCanvases(); // 실제 레이아웃 적용

            Assert.AreEqual(480, view.GetComponent<Canvas>().sortingOrder); // 레벨 표시 위로 기존 선택창 정렬 확인
            Image scrim = view.GetComponent<Image>(); // 전체 화면 가림막 조회
            Assert.AreEqual(scrim.color.r, scrim.color.g, 0.001f); // 전경과 분리된 중립 회색의 빨강·초록 일치 확인
            Assert.AreEqual(scrim.color.g, scrim.color.b, 0.001f); // 가림막 중립 회색의 초록·파랑 일치 확인
            Assert.Less(scrim.color.a, 1f); // 가림막 뒤 전투 화면의 은은한 표시 확인
            Assert.IsTrue(scrim.raycastTarget); // 다른 전투 조작의 모달 입력 차단 확인
            Assert.IsNotNull(view.transform.Find("Panel").GetComponent<Image>().sprite); // 가림막과 분리된 금속 패널 원화 확인
            RectTransform frame = (RectTransform)view.transform.Find("Panel"); // 중앙 패널의 영역 조회
            RectTransform plate = (RectTransform)view.transform.Find("RelicNameplate"); // 패널 밖의 실제 유물 명패 조회
            Assert.IsNotNull(plate.GetComponent<Image>().sprite); // 이미지 생성 명패가 실제 스프라이트로 연결됐는지 확인
            Assert.IsFalse(plate.GetComponent<Image>().raycastTarget); // 명패가 선택 입력을 막지 않는지 확인
            Assert.Greater(plate.anchoredPosition.y - plate.rect.height / 2f, frame.rect.height / 2f); // 명패가 프레임 밖 위에 놓였는지 확인
            Assert.AreEqual("서고 먼지갈피", plate.Find("RelicName").GetComponent<TMP_Text>().text); // 유물 이름의 전용 위치 확인
            Assert.AreEqual("가져올 카드 선택", view.transform.Find("Title").GetComponent<TMP_Text>().text); // 프레임 안의 분리된 선택 안내 확인
            Assert.IsNotNull(view.transform.Find("CancelChoice").GetComponent<Button>()); // 기존 최상위 직접 취소 버튼 유지 확인
            Transform shortChoice = view.transform.Find("Choice_0"); // 짧은 설명 후보 조회
            Transform longChoice = view.transform.Find("Choice_1"); // 긴 설명 후보 조회
            Assert.IsNull(shortChoice.Find("DescriptionViewport").GetComponent<ScrollRect>()); // 짧은 설명에는 스크롤 입력 미생성 확인
            RectTransform art = (RectTransform)longChoice.Find("Artwork"); // 긴 설명 카드의 별도 원화 영역 조회
            RectTransform viewport = (RectTransform)longChoice.Find("DescriptionViewport"); // 고정된 설명 표시 영역 조회
            Assert.Greater(art.anchoredPosition.y - art.rect.height / 2f, viewport.anchoredPosition.y + viewport.rect.height / 2f); // 설명과 원화 사이의 실제 공간 확인
            Assert.IsNotNull(viewport.GetComponent<RectMask2D>()); // 본문이 원화와 패널 밖에 그려지는 문제 차단 확인
            ScrollRect scroll = viewport.GetComponent<ScrollRect>(); // 넘치는 설명의 실제 스크롤 조회
            Assert.IsNotNull(scroll); // 긴 설명에만 스크롤 컴포넌트 연결 확인
            Assert.Greater(scroll.content.rect.height, viewport.rect.height); // 잘라 버리지 않고 스크롤할 전체 본문 크기 확인
            Assert.AreEqual(expanded.Description, scroll.content.GetComponent<TMP_Text>().text); // 긴 설명 원문 전체 유지 확인

            PointerEventData wheel = new PointerEventData(null) { scrollDelta = Vector2.down }; // 아래 휠 입력 생성
            ExecuteEvents.ExecuteHierarchy(viewport.gameObject, wheel, ExecuteEvents.scrollHandler); // 실제 휠 입력 경로 호출
            Assert.Greater(scroll.content.anchoredPosition.y, 0f); // 휠이 내용을 아래로 이동하는지 확인
            Assert.IsNull(chosen); // 휠이 카드 선택으로 이어지지 않음 확인
            Assert.AreSame(longChoice.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(viewport.gameObject)); // 본문 위의 일반 클릭은 기존 카드 버튼으로 전달 확인
            longChoice.GetComponent<Button>().onClick.Invoke(); // 긴 설명 카드의 원래 선택 콜백 실행
            Assert.AreSame(longCard, chosen); // 정확한 후보 카드 전달 확인
        } // 카드 레이아웃 검증 종료
        finally // 화면과 사본 정리
        { // 독립 자원 제거 시작
            Object.DestroyImmediate(canvas); // 시험 화면 제거
            Object.DestroyImmediate(expanded); // 시험 카드 사본 제거
        } // 독립 자원 제거 종료

        yield return new ExitPlayMode(); // 시험 플레이 상태 종료
    } // 후보 설명 스크롤 검사 종료
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
