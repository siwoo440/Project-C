using System; // 시간 제한과 조건 함수
using System.Collections; // 씬과 전투 연출 대기
using System.Collections.Generic; // 카드별 사용 횟수 기록
using System.Linq; // 실제 손패와 대상 조회
using System.Reflection; // 검사 전용 난수 설정
using NUnit.Framework; // 시작 조건과 결과 검증
using UnityEngine; // 게임 씬 객체와 난수 사용
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 턴 종료와 추가 선택 버튼
using Object = UnityEngine.Object; // 유니티 객체 검색

public sealed class FirstBossBalanceTests // 윈터폴 첫 보스 자동 전투 계측
{ // 첫 보스 계측 시작
    private static readonly int[] Seeds = { 20901, 20902, 20903 }; // 고정 탐사와 전투 시드 세 개
    private static readonly string[] PartyIds = { "ARC_00", "ARC_04", "ARC_02", "ARC_07" }; // 바보와 황제와 여사제와 전차 순서
    private const int RoundLimit = 24; // 측정할 마지막 플레이어 라운드

    [UnityTest] // 시드별 실제 전투 진행
    public IEnumerator FloorFiveBossUsesStartingDeck([ValueSource(nameof(Seeds))] int seed) // 재현 가능한 보스 승패와 턴 수 측정
    { // 시드 검사 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 자동 저장 분리 프로필 확인
        { // 실제 플레이어 저장 보호 시작
            Assert.Ignore("ProjectCVerification 검증 복제본에서 실행하세요."); // 일반 프로필 실행 생략
        } // 실제 플레이어 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 시작
        yield return Measure(seed); // 고정 시드 한 판 측정
        yield return new ExitPlayMode(); // 시드별 관리자와 전투 씬 해제
    } // 시드 검사 종료

    private static IEnumerator Measure(int seed) // 탐사 준비와 실제 보스 조우 생성
    { // 회차 준비 시작
        SceneManager.LoadScene("00_Boot"); // 부트 씬에서 정상 관리자 생성
        yield return WaitFor(() => Object.FindFirstObjectByType<MainMenuController>() != null && !SceneFlowManager.Instance.IsLoadingScene, "시작 메뉴"); // 시작 메뉴 준비 대기
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 출전 명단 조회
        int[] positions = PartyIds.Select(id => roster.Characters.ToList().FindIndex(member => member.CharacterId == id)).ToArray(); // 원본 식별자로 네 명의 위치 조회
        Assert.IsTrue(positions.All(index => index >= 0)); // 네 캐릭터의 실제 등록 확인
        PartyDepartureTestSupport.StartExploration(positions); // 정상 출전과 기본 덱 생성
        ExplorationSessionManager session = ExplorationSessionManager.Instance; // 이번 탐사의 진행 상태 조회
        while (session.CurrentFloor < 5) // 첫 보스 전까지의 탐사 구간 단축
        { // 층 전진 시작
            session.AdvanceFloor(); // 다음 층으로 정상 진행 상태 변경
        } // 층 전진 종료
        session.SetCurrentFloorSeed(seed); // 탐사 씬이 생성할 5층 지도 시드 설정
        yield return WaitFor(() => Object.FindFirstObjectByType<ExplorationRoomRoleRuntime>() != null && !SceneFlowManager.Instance.IsLoadingScene, "5층 지도"); // 실제 5층 방 역할 배치 대기
        yield return null; // 이전 조우 오브젝트의 프레임 정리 대기
        ExplorationMapRuntime map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 생성된 5층 지도 조회
        Assert.AreEqual(seed, map.CurrentMap.Seed); // 지정 시드로 생성된 지도 확인
        Vector2Int gate = map.CurrentMap.StairsCoordinate; // 실제 계단 관문 좌표 조회
        Assert.IsTrue(map.TryGetEncounterTypeAt(gate, out BattleType type) && type == BattleType.Boss); // 5층 관문의 보스 배치 확인
        string encounterId = $"F5_X{gate.x}_Y{gate.y}_S{seed}"; // 실제 관문 조우의 회차 식별자
        ExplorationEncounterView encounter = map.transform.Find("Encounter_" + encounterId).GetComponent<ExplorationEncounterView>(); // 5층에 배치된 조우 객체 조회
        UnityEngine.Random.State previousRandom = UnityEngine.Random.state; // 검사 전 게임 난수 상태 보관
        void SetDeckSeed(Scene scene, LoadSceneMode mode) // 전투 시작 함수 전 셔플 난수 고정
        { // 셔플 시드 설정 시작
            if (scene.name == "40_Battle") // 실제 전투 씬 확인
            { // 전투 초기화 직전 처리 시작
                BattleSceneSetup setup = Object.FindFirstObjectByType<BattleSceneSetup>(); // 이번 전투의 원본 관리자 조회
                Field(typeof(BattleSceneSetup), "useFixedShuffleSeed").SetValue(setup, true); // 기존 고정 시드 옵션 사용
                Field(typeof(BattleSceneSetup), "fixedShuffleSeed").SetValue(setup, seed); // 원본 덱 셔플의 시드 설정
            } // 전투 초기화 직전 처리 종료
        } // 셔플 시드 설정 종료
        SceneManager.sceneLoaded += SetDeckSeed; // 플레이 중 한정 셔플 설정 연결
        try // 씬 감시와 난수 복구 보장
        { // 보스전 진입 시작
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 탐사 아군의 실제 충돌체 조회
            encounter.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>()); // 관문 조우의 정상 접촉 경로 실행
            Assert.AreEqual(encounterId, session.ActiveRuntimeEncounterId); // 실제 관문에서 시작한 조우 확인
            yield return WaitFor(() => Object.FindFirstObjectByType<BattleSceneSetup>()?.IsInitialized == true && !SceneFlowManager.Instance.IsLoadingScene, "5층 보스전"); // 전투와 층 배율 준비 대기
            BattleSceneSetup battle = Object.FindFirstObjectByType<BattleSceneSetup>(); // 실제 5층 전투 관리자 조회
            BattleUnitRuntime boss = battle.EnemyUnits.Single(); // 등장한 보스 한 명 조회
            Assert.AreEqual("ENM_WF_ASHSTORM_CORE", boss.UnitId); // 실제 재폭풍의 심핵 식별자 확인
            Assert.AreEqual(BattleType.Boss, battle.BattleTurn.BattleType); // 보스전 등급 확인
            Assert.AreEqual(ExplorationDifficultyCalculator.ScaleHealth(boss.EnemySource.MaxHealth, 5, BattleType.Boss), boss.CurrentHealth); // 실제 5층 보스 체력 확인
            CollectionAssert.AreEqual(PartyIds, battle.AllyUnits.Select(unit => unit.UnitId)); // 출전 네 명과 편성 순서 확인
            Assert.IsTrue(battle.AllyUnits.All(unit => unit.CurrentHealth == unit.MaxHealth)); // 사전 피해 없는 출전 조건 확인
            Assert.AreEqual(12, battle.BattleDeck.CardCount); // 초기 덱 12장 확인
            Assert.AreEqual(8, battle.BattleDeck.HandCount); // 첫 손패 8장 확인
            Assert.AreEqual(6, battle.SharedActionPoints.CurrentActionPoints); // 첫 라운드 공용 행동력 확인
            Assert.AreEqual(0, RelicRunManager.EnsureInstance().Inventory.Count); // 유물 없는 초기 조건 확인
            Field(typeof(BattleEnemyActionRuntime), "random").SetValue(battle.EnemyActionRuntime, new System.Random(seed)); // 대상과 속도 추첨기만 고정
            battle.EnemyActionRuntime.SetBossPhaseStart(boss, 0); // 미실행 첫 예고의 패턴 순번 복구
            Assert.AreEqual(1, battle.EnemyActionRuntime.PrepareActions()); // 고정 난수로 첫 예고 한 번 생성
            Assert.AreEqual(1, battle.EnemyActionRuntime.FindAction(boss).PatternIndex); // 첫 페이즈 첫 행동 확인
            UnityEngine.Random.InitState(seed); // 실명과 추가 상태 확률의 난수 고정
            yield return Fight(battle, boss, seed); // 실제 턴과 카드 입력 진행
        } // 보스전 진입 종료
        finally // 검사 전 설정 복구
        { // 임시 상태 복구 시작
            SceneManager.sceneLoaded -= SetDeckSeed; // 씬 감시 해제
            UnityEngine.Random.state = previousRandom; // 게임 난수 상태 복구
        } // 임시 상태 복구 종료
    } // 회차 준비 종료

    private static IEnumerator Fight(BattleSceneSetup battle, BattleUnitRuntime boss, int seed) // 정상 카드와 턴으로 전투 결과 측정
    { // 전투 계측 시작
        BattleHandView hand = Object.FindFirstObjectByType<BattleHandView>(); // 실제 손패 화면 조회
        Button endTurn = hand.transform.Find("EndTurnButton").GetComponent<Button>(); // 실제 턴 종료 버튼 조회
        Dictionary<string, int> usage = new Dictionary<string, int>(); // 사용한 카드별 횟수
        List<string> minorCards = new List<string>(); // 레벨 상승으로 선택한 마이너 카드 순서
        int healed = 0; // 아군 실제 회복량 누계
        int incoming = 0; // 보스가 아군에게 가한 실제 체력 피해 누계
        int paid = 0; // 카드 선택 시 실제 지불한 행동력 누계
        int enemyTurns = 0; // 시작한 적 턴 횟수
        int hpAfterTwelve = -1; // 열두 번째 라운드 종료 시 보스 체력
        int startHp = boss.CurrentHealth; // 보스 시작 체력 보관
        int firstAttack = battle.EnemyActionRuntime.FindAction(boss).Amount; // 보스 첫 예고 공격력 보관
        double deadline = Time.realtimeSinceStartupAsDouble + 220d; // 연출 정지 감지 제한 시각
        using (battle.BattleEvents.Subscribe(BattleEventType.CardUsed, e => usage[e.Card.SourceData.CardId] = usage.TryGetValue(e.Card.SourceData.CardId, out int n) ? n + 1 : 1)) // 실제 해결된 카드 집계
        using (battle.BattleEvents.Subscribe(BattleEventType.HealingApplied, e => healed += e.TargetUnit.Team == BattleTeam.Ally ? e.AppliedAmount : 0)) // 실제 아군 회복량 집계
        using (battle.BattleEvents.Subscribe(BattleEventType.DamageApplied, e => incoming += e.SourceUnit == boss ? e.AppliedAmount : 0)) // 실제 보스 공격 피해 집계
        using (battle.BattleEvents.Subscribe(BattleEventType.TurnStarted, e => enemyTurns += e.Phase == BattleTurnPhase.EnemyTurn ? 1 : 0)) // 실제 적 턴 시작 집계
        { // 전투 반복 시작
            while (!battle.BattleTurn.IsBattleEnded && battle.BattleTurn.CurrentRound <= RoundLimit) // 승패 확정 또는 제한 라운드까지 진행
            { // 한 입력 처리 시작
                Assert.Less(Time.realtimeSinceStartupAsDouble, deadline, $"seed={seed} round={battle.BattleTurn.CurrentRound}"); // 실제 전투 진행 정지 감지
                if (!battle.BattleTurn.IsPlayerTurn || battle.GetComponent<BattleActionSequenceRunner>().IsBusy) // 적 턴 또는 행동 연출 확인
                { // 정상 연출 대기 시작
                    yield return null; // 적 행동과 카드 효과 완료 대기
                    continue; // 다음 입력 시점 확인
                } // 정상 연출 대기 종료
                if (battle.BattleTurn.CurrentRound == 13 && hpAfterTwelve < 0) // 열세 번째 플레이어 턴 첫 입력 확인
                { // 목표 종료점 기록 시작
                    hpAfterTwelve = boss.CurrentHealth; // 열두 턴 종료 보스 체력 보관
                } // 목표 종료점 기록 종료
                MinorCardSelectionView minor = Object.FindFirstObjectByType<MinorCardSelectionView>(); // 레벨 상승 보상 선택 화면 조회
                Button minorButton = minor == null ? null : minor.GetComponentsInChildren<Button>().FirstOrDefault(button => button.name.StartsWith("Minor_", StringComparison.Ordinal)); // 표시된 마이너 보상 첫 후보 조회
                if (minorButton != null) // 일반 입력을 가리는 레벨 보상 선택 확인
                { // 마이너 카드 선택 시작
                    minorCards.Add(minorButton.name.Substring("Minor_".Length)); // 실제 선택한 보상 식별자 기록
                    minorButton.onClick.Invoke(); // 정상 레벨 보상 선택 및 효과 적용
                    yield return null; // 이전 보상 버튼 제거 대기
                    continue; // 다음 보상 또는 손패로 이동
                } // 마이너 카드 선택 종료
                BattleDrawChoiceView choice = Object.FindFirstObjectByType<BattleDrawChoiceView>(); // 바보의 추가 드로우 선택 화면 확인
                BattleRemainingCardChoiceView remaining = Object.FindFirstObjectByType<BattleRemainingCardChoiceView>(); // 바보의 남은 한 장 처리 화면 확인
                if (choice != null || remaining != null) // 선택이 끝나지 않은 드로우 확인
                { // 추가 선택 입력 시작
                    Button button = choice != null ? choice.transform.Find("Choice_0").GetComponent<Button>() : remaining.transform.Find("KeepRemaining").GetComponent<Button>(); // 첫 후보 또는 맨 위 유지 선택
                    button.onClick.Invoke(); // 실제 선택 버튼 실행
                    yield return null; // 선택 화면 갱신 대기
                    continue; // 갱신한 손패로 재판단
                } // 추가 선택 입력 종료
                BattleUnitRuntime target = null; // 선택할 아군 또는 적 대상 준비
                CardInstance card = Choose(battle, boss, out target); // 현재 손패의 우선 행동 결정
                if (card == null) // 유효하고 지불할 수 있는 행동 부재 확인
                { // 턴 종료 입력 시작
                    Assert.IsTrue(endTurn.interactable); // 정상 턴 종료 가능 여부 확인
                    endTurn.onClick.Invoke(); // 실제 적 턴과 AI 행동 요청
                } // 턴 종료 입력 종료
                else // 현재 카드와 대상 사용
                { // 실제 카드 입력 시작
                    int ap = battle.SharedActionPoints.CurrentActionPoints; // 입력 전 공용 행동력 기록
                    hand.GetComponentsInChildren<BattleCardView>().Single(view => view.RuntimeCard == card).OnPointerClick(null); // 실제 손패의 카드 선택
                    if (card.TargetType == CardTargetType.SingleEnemy || card.TargetType == CardTargetType.SingleAlly) // 직접 대상 지정 카드 확인
                    { // 실제 대상 입력 시작
                        Object.FindObjectsByType<BattleUnitView>(FindObjectsSortMode.None).Single(view => view.RuntimeUnit == target).OnPointerClick(null); // 유효한 전투 유닛 선택
                    } // 실제 대상 입력 종료
                    Assert.AreEqual(ap - card.ApCost, battle.SharedActionPoints.CurrentActionPoints, card.SourceData.CardId); // 카드의 정상 행동력 지불 확인
                    paid += ap - battle.SharedActionPoints.CurrentActionPoints; // 사용 행동력 누계 반영
                } // 실제 카드 입력 종료
                yield return null; // 다음 입력 프레임 대기
            } // 한 입력 처리 종료
        } // 전투 반복 종료
        string outcome = battle.BattleTurn.IsBattleEnded ? battle.BattleTurn.Result.ToString() : "RoundLimit"; // 승리와 패배 및 관찰 중단 구분
        int rounds = Math.Min(battle.BattleTurn.CurrentRound, RoundLimit); // 실제 관찰한 플레이어 턴 수
        int alive = battle.AllyUnits.Count(unit => !unit.IsDead); // 종료 시 생존 인원 집계
        string health = string.Join("/", battle.AllyUnits.Select(unit => unit.CurrentHealth)); // 편성 순서별 종료 체력 정리
        string cards = string.Join(",", usage.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value)); // 카드별 실제 사용 기록 정리
        Assert.Greater(usage.Values.Sum(), 0); // 실제 카드 해결 확인
        Assert.Greater(incoming, 0); // 실제 보스 공격 피해 발생 확인
        Assert.Greater(minorCards.Count, 0); // 레벨 상승 선택 창 정상 처리 확인
        Debug.Log($"[Day209] seed={seed} outcome={outcome} rounds={rounds} enemyTurns={enemyTurns} startBossHP={startHp} firstAttack={firstAttack} bossHP12={hpAfterTwelve} bossHP={boss.CurrentHealth} alive={alive} allyHP={health} healed={healed} bossDamage={incoming} cards={usage.Values.Sum()} paidAP={paid} minors={string.Join(",", minorCards)} usage={cards}"); // 재현 가능한 전투 측정 결과 출력
    } // 전투 계측 종료

    private static CardInstance Choose(BattleSceneSetup battle, BattleUnitRuntime boss, out BattleUnitRuntime target) // 현재 손패와 공개 예고 기반 선택
    { // 선택 규칙 시작
        BattleUnitRuntime wounded = battle.AllyUnits.Where(unit => !unit.IsDead).OrderBy(unit => (float)unit.CurrentHealth / unit.MaxHealth).First(); // 체력 비율이 가장 낮은 생존 아군 선택
        BattleUnitRuntime threatened = battle.EnemyActionRuntime.FindAction(boss)?.Target; // 보스의 현재 예고 대상 조회
        CardInstance best = null; // 현재 최우선 카드 준비
        int bestScore = 0; // 유효한 행동의 우선순위 기준
        target = null; // 선택 대상 초기화
        foreach (CardInstance card in battle.BattleDeck.Hand) // 손패 앞쪽부터 같은 우선순위 처리
        { // 카드 후보 평가 시작
            if (card.OwnerUnit.IsDead || !battle.SharedActionPoints.CanSpend(card.ApCost)) // 사망 소유자와 비용 부족 확인
            { // 지불 불가 후보 제외 시작
                continue; // 다음 손패 확인
            } // 지불 불가 후보 제외 종료
            int score = 0; // 기본 미사용 우선순위
            BattleUnitRuntime recipient = boss; // 공격 카드 기본 대상 설정
            switch (card.EffectType) // 시작 덱 카드의 실제 효과별 선택
            { // 효과별 우선도 시작
                case CardEffectType.Heal: // 아군 회복 우선도
                    score = wounded.MaxHealth - wounded.CurrentHealth >= card.EffectValue ? 90 : 0; // 표기 회복량 이상 다친 아군 우선
                    recipient = wounded; // 최저 체력 비율 아군 지정
                    break; // 회복 평가 종료
                case CardEffectType.Damage: // 보스 공격 우선도
                    score = card.SourceData.CardId == "CRD_ARC_07_MARK" ? 80 : 60 + card.EffectValue; // 전차 표식 다음 표기 공격력 순서
                    break; // 공격 평가 종료
                case CardEffectType.ChooseDraw: // 바보 선택 드로우 우선도
                    score = battle.BattleDeck.HasDrawableCards ? 50 : 0; // 뽑을 후보가 있을 때만 사용
                    recipient = card.OwnerUnit; // 바보 본인 대상 설정
                    break; // 선택 드로우 평가 종료
                case CardEffectType.ProtectAttackTarget: // 여사제의 예고 대상 보호
                case CardEffectType.ApplyStatusEffect: // 역할 카드의 물리 방어 강화
                    score = threatened != null ? 30 : 0; // 이번 공격 대상에 방어 효과 사용
                    recipient = threatened; // 현재 공격 예고 대상 지정
                    break; // 예고 대상 보호 평가 종료
                case CardEffectType.RedirectAttackAndDefend: // 황제의 보스 공격 유도
                    score = threatened != null && card.OwnerUnit.CurrentHealth > threatened.CurrentHealth && battle.EnemyActionRuntime.CanRedirectAttack(boss, card.OwnerUnit) ? 20 : 0; // 황제 체력이 더 높을 때만 유도
                    break; // 공격 유도 평가 종료
            } // 효과별 우선도 종료
            if (score > bestScore) // 현재 후보의 더 높은 우선도 확인
            { // 최우선 행동 교체 시작
                bestScore = score; // 새로운 우선도 기록
                best = card; // 사용할 카드 보관
                target = recipient; // 사용할 대상 보관
            } // 최우선 행동 교체 종료
        } // 카드 후보 평가 종료
        return best; // 카드 또는 턴 종료 선택 반환
    } // 선택 규칙 종료

    private static FieldInfo Field(Type type, string name) // 검사에 쓰는 원본 난수 필드 조회
    { // 난수 필드 조회 시작
        FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); // 원본 비공개 필드 조회
        Assert.IsNotNull(field, type.Name + "." + name); // 난수 설정 변경 시 명시적 실패 확인
        return field; // 확인한 난수 설정 반환
    } // 난수 필드 조회 종료

    private static IEnumerator WaitFor(Func<bool> condition, string label) // 실제 씬과 지도 준비 대기
    { // 준비 대기 시작
        double deadline = Time.realtimeSinceStartupAsDouble + 35d; // 실제 준비 제한 시각
        while (!condition()) // 준비 조건을 만족할 때까지 반복
        { // 게임 프레임 대기 시작
            Assert.Less(Time.realtimeSinceStartupAsDouble, deadline, label); // 씬 준비 시간 초과 진단
            yield return null; // 다음 플레이 프레임 대기
        } // 게임 프레임 대기 종료
    } // 준비 대기 종료
} // 첫 보스 계측 종료
