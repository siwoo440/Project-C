using System.Collections; // 실제 UI 갱신 프레임 대기
using NUnit.Framework; // 상태 상세 회귀 검증
using TMPro; // 실제 상세 텍스트 조회
using UnityEngine; // 독립 전투 UI 생성
using UnityEngine.EventSystems; // 휠 및 클릭 이벤트 전달
using UnityEngine.TestTools; // 실제 플레이 모드 검사
using UnityEngine.UI; // 클리핑과 스크롤 구성 조회

public sealed class BattleStatusTooltipScrollTests // 전투 상태 상세 스크롤과 입력 회귀 검사
{ // 상태 상세 검사 시작
    [UnityTest] // 상태 상세 내용과 스크롤 입력 검사 등록
    public IEnumerator StatusTooltip_InsetsTitleAndScrollsClippedOverflow() // 장식 안쪽 정렬과 넘친 본문의 휠·바 스크롤 확인
    { // 본문 스크롤 검사 시작
        yield return new EnterPlayMode(); // 실제 UI 컴포넌트 구동
        GameObject canvas = new GameObject("StatusScrollCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 시험 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 시험 캔버스 유지
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 정보 생성

        try // 테스트 종료 후 시험 객체 정리 보장
        { // 상태 상세 화면 검사 시작
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // 실제 화면 좌표계 사용
            GameObject anchor = new GameObject("Unit", typeof(RectTransform), typeof(BattleStatusEffectView)); // 상태 화면 대상 생성
            anchor.transform.SetParent(canvas.transform, false); // 시험 캔버스에 전투 유닛 배치
            BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(character); // 실제 아군 런타임 생성
            unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 1); // 화면에 들어오는 첫 상태 등록
            BattleStatusEffectView status = anchor.GetComponent<BattleStatusEffectView>(); // 실제 상태 화면 조회
            status.Bind(unit); // 아군 상태 변경 이벤트 연결
            status.ShowTooltip(); // 직접 호출로 상세 표시
            Canvas.ForceUpdateCanvases(); // 첫 표시의 UI 배치 즉시 반영

            RectTransform tooltip = status.TooltipRect; // 실제 전투 캔버스에 올라간 상태 상세창 조회
            Assert.AreEqual(canvas.transform, tooltip.parent); // 다른 전투 유닛보다 앞에 있는 상세 캔버스 부모 확인
            ScrollRect scroll = tooltip.GetComponent<ScrollRect>(); // 실제 스크롤 컴포넌트 조회
            Assert.IsNotNull(scroll); // 본문 스크롤 기능 확인
            TMP_Text title = tooltip.Find("TitleText").GetComponent<TMP_Text>(); // 고정 제목 조회
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>()); // 본문 가시 영역의 클리핑 확인
            Assert.GreaterOrEqual(title.rectTransform.offsetMin.x, 42f); // 왼쪽 장식보다 깊은 제목 시작 확인
            Assert.AreEqual(title.rectTransform.offsetMin.x, scroll.viewport.offsetMin.x); // 제목과 본문 시작점 정렬 확인
            Assert.Less(scroll.viewport.offsetMax.y, title.rectTransform.offsetMin.y); // 제목과 본문 사이 여백 확인
            Assert.IsFalse(scroll.verticalScrollbar.gameObject.activeSelf); // 짧은 본문의 스크롤바 숨김 확인

            unit.ApplyStatusEffect(BattleStatusEffectType.Regeneration, 1, 3, 1); // 두 번째 상세 상태 추가
            unit.ApplyStatusEffect(BattleStatusEffectType.Burn, 1, 3, 1); // 세 번째 상세 상태 추가
            unit.ApplyStatusEffect(BattleStatusEffectType.Blind, 1, 3, 1); // 네 번째 상세 상태 추가
            unit.ApplyStatusEffect(BattleStatusEffectType.AttackPowerUp, 1, 3, 1); // 다섯 번째 상세 상태 추가
            unit.ApplyStatusEffect(BattleStatusEffectType.PhysicalDefenseUp, 1, 3, 1); // 여섯 번째 상세 상태 추가
            yield return null; // 여러 상태의 글자 배치와 스크롤바 크기 갱신 대기
            Canvas.ForceUpdateCanvases(); // 스크롤 영역 배치 완료

            Assert.IsTrue(scroll.verticalScrollbar.gameObject.activeSelf); // 긴 본문의 수직 스크롤바 표시 확인
            Assert.IsTrue(scroll.verticalScrollbar.IsInteractable()); // 스크롤바 조작 가능 확인
            Assert.IsFalse(scroll.horizontal); // 가로 스크롤 비활성 확인
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height); // 클리핑 영역보다 긴 내용 확인
            Assert.AreEqual(TextOverflowModes.Overflow, scroll.content.GetComponent<TMP_Text>().overflowMode); // 말줄임 없는 전체 본문 확인
            Vector2 titlePosition = title.rectTransform.anchoredPosition; // 스크롤 전 고정 제목 위치 저장

            PointerEventData wheel = new PointerEventData(null) { scrollDelta = Vector2.down }; // 아래 방향 휠 이벤트 생성
            ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, wheel, ExecuteEvents.scrollHandler); // 실제 스크롤 핸들러에 휠 전달
            Assert.Greater(scroll.content.anchoredPosition.y, 0f); // 휠 입력에 따른 본문 이동 확인
            Assert.AreEqual(titlePosition, title.rectTransform.anchoredPosition); // 본문 이동과 독립적인 제목 위치 확인
            scroll.verticalScrollbar.value = 0.5f; // 손잡이 값을 변경해 본문 절반 위치로 이동
            Assert.AreEqual(0.5f, scroll.verticalNormalizedPosition, 0.01f); // 휠 갱신 프레임과 관계없는 손잡이 연결 확인
            scroll.verticalScrollbar.value = 0f; // 스크롤 손잡이를 맨 아래로 이동
            Assert.AreEqual(0f, scroll.verticalNormalizedPosition, 0.01f); // 손잡이 위치와 본문 스크롤 동기화 확인

            status.HideTooltip(); // 열린 상세창 닫기
            status.ShowTooltip(); // 상세창 다시 열기
            Assert.AreEqual(0f, scroll.content.anchoredPosition.y, 0.01f); // 새 열기에서 첫 내용 위치 복원 확인
        } // 상태 상세 화면 검사 종료
        finally // 시험 객체 정리
        { // 화면 정리 시작
            Object.DestroyImmediate(canvas); // 독립 캔버스 제거
            Object.DestroyImmediate(character); // 임시 캐릭터 정보 제거
        } // 화면 정리 종료

        yield return new ExitPlayMode(); // 시험 플레이 모드 종료
    } // 본문 스크롤 검사 종료

    [UnityTest] // 상세창 입력 경로 분리 검사 등록
    public IEnumerator StatusTooltip_RoutesPanelInputWithoutInterceptingCardDrags() // 상세 클릭 흡수와 전투 카드 드래그 통과 확인
    { // 상세 입력 검사 시작
        yield return new EnterPlayMode(); // 실제 레이캐스트 모듈 실행
        GameObject canvas = new GameObject("StatusInputCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 시험 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 시험 캔버스 유지
        GameObject card = new GameObject("DraggedCard", typeof(BattleCardDragHandler)); // 실제 드래그 입력 형식 준비
        Object.DontDestroyOnLoad(card); // 씬 전환 중 시험 카드 유지
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 정보 생성

        try // 테스트 종료 후 시험 객체 정리 보장
        { // 상세 입력 확인 시작
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // 실제 화면 좌표계 사용
            GameObject anchor = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 실제 유닛 입력 대상 생성
            anchor.transform.SetParent(canvas.transform, false); // 시험 캔버스에 유닛 배치
            BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(character); // 실제 아군 런타임 생성
            unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 1, 3, 1); // 표시할 중독 상태 등록
            anchor.GetComponent<BattleUnitView>().Bind(unit); // 전투 유닛과 UI 연결
            anchor.GetComponent<BattleStatusEffectView>().ShowTooltip(); // 실제 상태 상세 표시
            yield return null; // 중첩 캔버스가 그리기 순서를 계산할 프레임 대기
            Canvas.ForceUpdateCanvases(); // 화면 배치와 레이캐스트 영역 갱신

            RectTransform tooltip = anchor.GetComponent<BattleStatusEffectView>().TooltipRect; // 원래 유닛 화면이 소유한 실제 상세 영역 조회
            RectTransform viewport = tooltip.GetComponent<ScrollRect>().viewport; // 본문 입력 영역 조회
            Assert.IsNotNull(tooltip.GetComponent<BattleStatusEffectTooltipRaycaster>()); // 상세 전용 레이캐스터 설치 확인
            Assert.AreEqual(tooltip.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(viewport.gameObject)); // 상세 클릭의 유닛 클릭 전달 방지 확인
            Assert.AreEqual(anchor, ExecuteEvents.GetEventHandler<IPointerClickHandler>(anchor)); // 원래 전투 유닛 클릭 경로 확인
            PointerEventData pointer = new PointerEventData(null) // 투명 본문 내부의 포인터 자료 생성
            { // 포인터 좌표 지정 시작
                position = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(viewport.rect.center)) // 본문 중심 화면 좌표 지정
            }; // 포인터 좌표 지정 종료
            Assert.IsFalse(BattleStatusEffectTooltipRaycaster.ShouldPassThrough(pointer)); // 일반 상세 그래픽을 계속 탐색하는 입력 확인
            pointer.pointerDrag = card; // 포인터의 카드 드래그 대상 지정
            pointer.dragging = true; // 실제 드래그 중인 입력 상태 설정
            Assert.IsTrue(BattleStatusEffectTooltipRaycaster.ShouldPassThrough(pointer)); // 카드의 기존 전투 대상 선택 경로 보존 확인
        } // 상세 입력 확인 종료
        finally // 시험 객체 정리
        { // 화면 정리 시작
            Object.DestroyImmediate(canvas); // 독립 캔버스 제거
            Object.DestroyImmediate(card); // 임시 드래그 카드 제거
            Object.DestroyImmediate(character); // 임시 캐릭터 정보 제거
        } // 화면 정리 종료

        yield return new ExitPlayMode(); // 시험 플레이 모드 종료
    } // 상세 입력 검사 종료
} // 상태 상세 검사 종료
