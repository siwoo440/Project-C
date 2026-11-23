using System.Collections; // 실제 전투 화면 프레임 대기 사용
using System.Collections.Generic; // 독립 설정과 카드 목록 사용
using System.Reflection; // 독립 설정 관리자 교체 사용
using NUnit.Framework; // 실제 전투 표시 검사 사용
using UnityEngine; // 실제 전투 화면 객체 사용
using UnityEngine.InputSystem; // 임시 키보드 입력 사용
using UnityEngine.InputSystem.LowLevel; // 임시 키 입력 상태 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 손패 배경 사용
public sealed class BattleDisplaySettingsViewTests // 실제 전투 표시 설정 검사
{ // 전투 표시 검사 시작
    [UnityTest] // 실제 피해와 툴팁 화면 검사 지정
    public IEnumerator DisplaySettings_ControlDamageAndHoveredTooltips() // 피해 숫자와 카드·상태 툴팁의 실제 표시 확인
    { // 실제 전투 화면 검사 시작
        yield return new EnterPlayMode(); // 실제 화면 플레이 시작
        FieldInfo runtimeField = typeof(GraphicsAudioSettingsRuntime).GetField("current", BindingFlags.Static | BindingFlags.NonPublic); // 전역 설정 저장 필드 조회
        object oldSettings = runtimeField.GetValue(null); // 기존 전역 설정 보관
        GraphicsAudioSettings settings = new GraphicsAudioSettings(new MemoryStore(), new NoOpApplier()); // 독립 전투 표시 설정 생성
        runtimeField.SetValue(null, settings); // 독립 설정을 실제 화면에 연결
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 실제 사용자 키보드와 분리된 임시 키보드 생성
        GameObject canvas = new GameObject("DisplaySettingsCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 독립 화면 유지
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>(); // 임시 아군 원본 생성
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>(); // 임시 적 원본 생성
        DeckData source = ScriptableObject.CreateInstance<DeckData>(); // 임시 카드 덱 원본 생성
        BattleTurnRuntime turns = null; // 전투 턴 관리자 초기화
        BattleHandView hand = null; // 손패 화면 초기화
        try // 사용자 설정과 임시 객체 복구 보장
        { // 실제 전투 화면 검사 시작
            BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(character); // 실제 아군 유닛 생성
            BattleUnitRuntime enemy = BattleUnitRuntime.CreateEnemy(enemyData); // 실제 적 유닛 생성
            GameObject unitObject = new GameObject("Unit", typeof(RectTransform), typeof(BattleUnitView)); // 실제 전투 유닛 화면 생성
            unitObject.transform.SetParent(canvas.transform, false); // 전투 유닛 화면 연결
            BattleUnitView unitView = unitObject.GetComponent<BattleUnitView>(); // 실제 전투 유닛 화면 조회
            unitView.Bind(ally); // 실제 아군 상태 연결
            ally.ApplyStatusEffect(BattleStatusEffectType.Poison, 3, 2, 1); // 상세 표시용 중독 적용
            BattleStatusEffectView status = unitObject.GetComponent<BattleStatusEffectView>(); // 실제 상태 상세 화면 조회
            Assert.IsTrue(settings.Change(2, 9, 1)); // 상세 툴팁을 키 누름 방식으로 변경
            unitView.OnPointerEnter(null); // 실제 유닛 마우스 진입 전달
            Assert.IsFalse(status.IsTooltipVisible); // 키를 누르기 전 상태 상세 숨김 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftAlt)); // 임시 Alt 키 누름 전달
            InputSystem.Update(); // 키보드 입력 상태 반영
            Assert.IsTrue(BattleDisplaySettings.IsTooltipKeyPressed()); // 실제 상세 키 입력 상태 확인
            typeof(BattleUnitView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unitView, null); // 실제 유닛의 프레임별 화면 갱신 실행
            Assert.IsTrue(status.IsTooltipVisible); // 입력 직후 상태 상세 표시 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 임시 Alt 키 떼기 전달
            InputSystem.Update(); // 키보드 입력 상태 반영
            typeof(BattleUnitView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unitView, null); // 실제 유닛의 프레임별 화면 갱신 실행
            Assert.IsFalse(status.IsTooltipVisible); // 키를 떼면 상태 상세 숨김 확인
            CardData cardData = Resources.Load<CardData>("StarterCards/Attack"); // 실제 공격 카드 원본 조회
            CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 카드 소유자 목록 조회
            CharacterData ownerData = roster.Characters[0]; // 유효한 카드 소유자 조회
            BattleUnitRuntime owner = BattleUnitRuntime.CreateAlly(ownerData); // 실제 카드 소유자 생성
            List<IDeckCardEntry> entries = new List<IDeckCardEntry> { new RunDeckCardEntry(cardData, ownerData) }; // 실제 공격 카드 항목 구성
            BattleDeckRuntime deck = BattleDeckRuntime.Create(source, entries, new[] { owner }, 5, 1); // 실제 손패 덱 생성
            BattleActionPointRuntime points = new BattleActionPointRuntime(6); // 실제 공용 행동력 생성
            turns = new BattleTurnRuntime(deck, points, new[] { owner }, new[] { enemy }, 0, BattleType.Normal); // 독립 전투 턴 생성
            deck.DrawCards(1); // 실제 카드를 손패로 이동
            Assert.IsTrue(turns.StartBattle(0)); // 실제 아군 턴 시작 확인
            GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
            handObject.transform.SetParent(canvas.transform, false); // 독립 전투 캔버스에 손패 연결
            hand = handObject.GetComponent<BattleHandView>(); // 실제 손패 화면 조회
            Assert.IsTrue(hand.Bind(deck, points, turns)); // 실제 카드 덱 연결 확인
            BattleCardView card = handObject.GetComponentInChildren<BattleCardView>(); // 실제 손패 카드 화면 조회
            Assert.IsNotNull(card); // 실제 손패 카드 존재 확인
            card.OnPointerEnter(null); // 실제 카드 마우스 진입 전달
            GameObject cardTooltip = handObject.transform.Find("CardTooltip").gameObject; // 실제 카드 상세 화면 조회
            Assert.IsFalse(cardTooltip.activeSelf); // 키를 누르기 전 카드 상세 숨김 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftAlt)); // 임시 Alt 키 누름 전달
            InputSystem.Update(); // 키보드 입력 상태 반영
            typeof(BattleHandView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hand, null); // 실제 손패의 프레임별 화면 갱신 실행
            Assert.IsTrue(cardTooltip.activeSelf); // 마우스 진입 중 Alt로 카드 상세 표시 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 임시 Alt 키 떼기 전달
            InputSystem.Update(); // 키보드 입력 상태 반영
            typeof(BattleHandView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hand, null); // 실제 손패의 프레임별 화면 갱신 실행
            Assert.IsFalse(cardTooltip.activeSelf); // 키를 떼면 카드 상세 숨김 확인
            Assert.IsTrue(settings.Change(2, 8, 1)); // 피해 숫자 표시 끄기
            BattleCombatFeedbackView feedback = unitObject.GetComponent<BattleCombatFeedbackView>(); // 실제 전투 피드백 화면 조회
            feedback.ShowDamage(new BattleDamageResult(10, 0, 10, 10, BattleDamageType.Physical)); // 실제 피해 피드백 실행
            Assert.IsNull(unitObject.transform.Find("FloatingCombatText")); // 피해 숫자 비표시 확인
            feedback.ShowHealing(5); // 실제 회복 피드백 실행
            Assert.IsNotNull(unitObject.transform.Find("FloatingCombatText")); // 피해 숫자 끄기와 무관한 회복 숫자 확인
        } // 실제 전투 화면 검사 종료
        finally // 사용자 설정과 임시 객체 정리
        { // 사용자 상태 복구 시작
            hand?.Unbind(); // 실제 손패 이벤트 연결 해제
            turns?.Dispose(); // 전투 턴 이벤트 연결 해제
            Object.DestroyImmediate(canvas); // 독립 전투 화면 제거
            Object.DestroyImmediate(character); // 임시 아군 원본 제거
            Object.DestroyImmediate(enemyData); // 임시 적 원본 제거
            Object.DestroyImmediate(source); // 임시 덱 원본 제거
            InputSystem.RemoveDevice(keyboard); // 임시 키보드 장치 제거
            runtimeField.SetValue(null, oldSettings); // 기존 사용자 설정 복구
        } // 사용자 상태 복구 종료
        yield return new ExitPlayMode(); // 실제 화면 플레이 종료
    } // 실제 전투 화면 검사 종료
    private sealed class MemoryStore : ISettingsStore // 독립 전투 설정 저장소
    { // 독립 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 임시 설정 값 목록
        public int Read(string key, int fallback) // 임시 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 임시 설정 값 저장
        { // 설정 저장 시작
            values[key] = value; // 지정 설정 값 기록
        } // 설정 저장 종료
    } // 독립 저장소 종료
    private sealed class NoOpApplier : ISettingsApplier // 독립 전투 설정 적용기
    { // 독립 적용기 시작
        public void Apply(string key, GraphicsAudioSettings settings) // 설정 적용 요청 수신
        { // 설정 적용 시작
        } // 설정 적용 종료
    } // 독립 적용기 종료
} // 전투 표시 검사 종료
