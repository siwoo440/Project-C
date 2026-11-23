using System.Collections; // 실제 전투 씬 진입 대기
using System.Collections.Generic; // 원문 선택지 목록
using System.Linq; // 조우 후보 확인
using UnityEngine; // 원본 그림과 게임 객체 조회
using UnityEngine.SceneManagement; // 현재 씬 확인

public static class BurntCartEvent // 불탄 수레의 잔해 원문과 선택 처리
{ // 수레 이벤트 시작
    public const string Id = "winterfall_burnt_cart"; // 수레 이벤트 원본 식별자
    public const int Box = 0; // 상자 열기 선택
    public const int Salvage = 1; // 수레 해체 선택
    public const int Mark = 2; // 표식 남기기 선택
    private const int SupplyChance = 60; // 수레 상자의 원래 물자 확률
    private const int AmbushChance = 100 - SupplyChance; // 회색 로프가 없을 때의 강제 매복 확률
    private static bool resolving; // 상태 변경 중 재진입 방지
    public static EnemyData Hunter => Resources.Load<EncounterData>("Encounters/Encounter_WF_AshHunter")?.Enemies[0]; // 실제 잿풍 사냥꾼 원본 조회

    internal static ExplorationEventData Create() // 기획서 본문과 원화로 수레 방 생성
    { // 이벤트 원본 생성 시작
        var choices = new List<ExplorationEventChoiceData> // 원래 순서의 선택지 준비
        { // 수레 선택지 시작
            Choice("상자를 연다", "뚜껑이 열리는 순간, 재가 안쪽으로 빨려 들어간다.\n상자 안에는 멀쩡한 물자… 혹은 텅 빈 공간.\n텅 비어 있다면, 바로 그때 수레 아래에서 재가 솟구치며 숨은 발톱이 튀어나온다."), // 상자 선택 결말
            Choice("수레를 해체한다", "녹슨 금속이 손을 긁는다.\n피가 떨어지자 재가 잠깐 더 짙게 변한다.\n그래도 당신은 필요한 부품을 모아 주머니에 넣는다.\n생존은 늘 이런 식이다."), // 해체 선택 결말
            Choice("표식을 남기고 간다", "벽면에 작은 표식을 남기자, 이상하게도 마음이 안정된다.\n“여긴 위험하지만, 쓸모가 있다.”\n그 판단이 당신을 현실로 붙잡는다.") // 표식 선택 결말
        }; // 수레 선택지 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 새 탐사 이벤트 원본 생성
        data.Initialize(Id, "불탄 수레의 잔해", "반쯤 탄 수레가 길을 막고 있다.\n수레 안에는 식량 상자처럼 보이는 것이 남아 있고, 재 속에는 금속 공구도 섞여 있다.\n하지만 수레 아래쪽에는… 무언가가 기다린 흔적이 있다.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/BurntCart"), choices); // 원문 장면과 원화 연결
        return data; // 수레 이벤트 후보 반환
    } // 이벤트 원본 생성 종료

    private static ExplorationEventChoiceData Choice(string title, string story) // 행동과 원문 결말 연결
    { // 선택지 생성 시작
        var result = new ExplorationEventChoiceData(); // 실제 행동 버튼 데이터 생성
        result.Initialize(title, story, null); // 행동 이름과 결말 보관
        return result; // 실제 선택지 반환
    } // 선택지 생성 종료

    private static System.Random BoxRandom(string room) // 같은 상자의 저장 복원 추첨 유지
    { // 방 전용 난수 생성 시작
        return new System.Random(ExplorationTreasureRewardService.BuildStableHash(room)); // 이벤트 방 식별자에서 고정 난수 생성
    } // 방 전용 난수 생성 종료

    public static bool HasSupplies(string room) // 상자 물자 당첨 여부 조회
    { // 상자 추첨 시작
        return BoxRandom(room).Next(100) < SupplyChance; // 유물과 독립된 오십구까지의 물자 지급
    } // 상자 추첨 종료
    internal static string BoxChoiceEffects() // 수레 상자의 실제 추첨 확률 표시
    { // 상자 안내 시작
        int avoided = GetAvoidedAmbushChance(); // 현재 회차의 매복 감소 퍼센트포인트
        string result = $"철판·나사 각 1개 {SupplyChance}% / 사냥꾼 추가 매복 {AmbushChance - avoided}%"; // 물자와 실제 매복 확률 표시
        return avoided > 0 ? result + $" / 매복 회피 {avoided}%" : result; // 회색 로프의 빈 상자 결과 추가
    } // 상자 안내 종료
    private static int GetAvoidedAmbushChance() // 보유 유물의 수레 매복 확률 감소량
    { // 매복 감소 조회 시작
        int reduction = RelicRunManager.Instance?.Inventory?.FindEffect(RelicEffectType.ReducedForcedEncounterChance)?.EffectValue ?? 0; // 실제 획득한 회색 로프의 비율
        return Mathf.Clamp(reduction, 0, AmbushChance); // 물자 확률을 건드리지 않는 매복 감소 범위
    } // 매복 감소 조회 종료

    internal static EncounterData FindNormal(string id, int version) // 저장한 일반 조우 원본 검색
    { // 조우 원본 조회 시작
        return string.IsNullOrEmpty(id) ? null : Resources.LoadAll<EncounterData>("Encounters").FirstOrDefault(e => e.EncounterId == id && e.BattleType == BattleType.Normal && e.IsAvailableForPoolVersion(version) && e.IsValidData()); // 저장 버전의 정상 일반 조우 확인
    } // 조우 원본 조회 종료

    private static EncounterData PickNormal(string room, ExplorationSessionManager session) // 현재 층 일반 조우에 추가할 기본 편성 선택
    { // 기본 편성 추첨 시작
        var map = Object.FindFirstObjectByType<ExplorationMapRuntime>(); // 현재 층 실물 조우 조회
        var candidates = new List<EncounterData>(); // 실제 일반 조우 후보 준비
        if (map != null && map.CurrentFloor == session.CurrentFloor) // 플레이 중인 동일 층 확인
        { // 지도 조우 검색 시작
            foreach (var view in map.GetComponentsInChildren<ExplorationEncounterView>().OrderBy(item => item.name, System.StringComparer.Ordinal)) // 실제 방 식별자 순서로 탐색
            { // 지도 조우 검사 시작
                var original = view.Data; // 현재 방의 기존 적 편성 조회
                if (original != null && original.BattleType == BattleType.Normal && original.IsAvailableForPoolVersion(session.EncounterPoolVersion)) // 현재 층의 일반 전투 후보 확인
                { // 일반 후보 추가 시작
                    candidates.Add(original); // 실제 배치된 조우 원본 보관
                } // 일반 후보 추가 종료
            } // 지도 조우 검사 종료
        } // 지도 조우 검색 종료
        if (candidates.Count == 0) // 현재 층의 일반 방을 모두 클리어한 경우 확인
        { // 같은 층 후보 목록 조회 시작
            candidates.AddRange(Resources.LoadAll<EncounterData>("Encounters").Where(e => e.IsValidData() && e.BattleType == BattleType.Normal && e.IsAvailableForPoolVersion(session.EncounterPoolVersion)).OrderBy(e => e.EncounterId, System.StringComparer.Ordinal)); // 해당 층 버전의 일반 조우 목록 복구
        } // 같은 층 후보 목록 조회 종료
        return candidates.Count == 0 ? null : candidates[BoxRandom(room + "_normal").Next(candidates.Count)]; // 고정 방 추첨으로 한 편성 선택
    } // 기본 편성 추첨 종료

    public static bool TryResolve(string room, int choice, CharacterData actor, out string summary) // 미처리 수레 방의 행동 한 번 확정
    { // 행동 처리 시작
        summary = string.Empty; // 선택 결과 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 층과 이벤트 상태 조회
        var party = BattleResultManager.EnsureInstance(); // 출전 생존자 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(room) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(room) || session.Cart.AmbushRoom != null || choice < Box || choice > Mark || party.GetLivingAllyCount() < 1) // 현재 탐사의 새 선택 여부 확인
        { // 중복과 무효 선택 차단 시작
            return false; // 회차 상태와 보상 보존
        } // 중복과 무효 선택 차단 종료
        if (choice == Salvage && !party.IsLivingPartyMember(actor)) // 해체할 생존자 확인
        { // 미선택 처리 시작
            return false; // 체력과 물자 보존
        } // 미선택 처리 종료
        if (choice == Mark && session.Cart.HasMark) // 미사용 표식 보유 확인
        { // 기존 표식 보존 시작
            summary = "아직 사용하지 않은 상점 표식이 있습니다. 다른 행동을 선택하세요."; // 중복 표식 선택 안내
            return false; // 기존 표식과 현재 방 보존
        } // 기존 표식 보존 종료
        resolving = true; // 자원 알림 중 중복 처리 잠금
        try // 처리 후 잠금 해제 보장
        { // 확정 행동 시작
            int boxRoll = choice == Box ? BoxRandom(room).Next(100) : 0; // 상자 행동에서만 기존 고정 난수 한 번 조회
            if (choice == Mark) // 다음 상점 할인 예약 확인
            { // 표식 남기기 시작
                session.Cart.SetMark(room); // 다음 상점의 첫 성공 구매에 할인 예약
                summary = "다음 상점에서 처음 구매하는 상품 하나의 가격 10% 할인"; // 적용 조건 표시
            } // 표식 남기기 종료
            else if (choice == Salvage) // 생존자 해체 비용 확인
            { // 해체 처리 시작
                party.TryGetSavedAllyState(actor, out int health, out _, out _); // 해체 전 체력 조회
                party.ApplyEventChange(actor, 3, 0); // 지정 생존자 체력 세 점 감소
                PlayerResourceManager.EnsureInstance().AddResources(0, 1, 1, 1); // 나사와 철판과 전선 한 개씩 지급
                summary = $"{actor.DisplayName} 체력 {health} → {Mathf.Max(0, health - 3)} · 철판 +1 · 나사 +1 · 전선 +1"; // 실제 비용과 보상 표시
            } // 해체 처리 종료
            else if (boxRoll < SupplyChance) // 기존의 변경 없는 물자 당첨 구간 확인
            { // 물자 획득 시작
                PlayerResourceManager.EnsureInstance().AddResources(0, 1, 1, 0); // 나사와 철판 한 개씩 지급
                summary = "철판 +1 · 나사 +1"; // 물자 결과 표시
            } // 물자 획득 종료
            else if (boxRoll < SupplyChance + GetAvoidedAmbushChance()) // 회색 로프로 줄인 매복 확률 구간 확인
            { // 강제 매복 회피 시작
                summary = "회색 로프로 매복을 피했습니다. 상자는 비어 있었습니다."; // 전투와 물자 보상 없는 확정 안내
            } // 강제 매복 회피 종료
            else // 사냥꾼 매복 당첨
            { // 매복 준비 시작
                var normal = PickNormal(room, session); // 현재 층의 일반 적 편성 선택
                if (normal == null || Hunter == null) // 실제 조우와 사냥꾼 원본 연결 확인
                { // 원본 누락 처리 시작
                    summary = "매복 전투를 준비하지 못했습니다."; // 미확정 안내
                    return false; // 이벤트 방 재선택 허용
                } // 원본 누락 처리 종료
                session.Cart.SetAmbush(room, normal.EncounterId); // 전투 전 저장에 일반 편성과 방 기록
                summary = $"{normal.DisplayName} · 잿풍의 사냥꾼 1마리 추가 매복"; // 확인 후 전투 진입 안내
            } // 매복 준비 종료
            session.MarkEventResolved(room, choice != Box || boxRoll < SupplyChance); // 물자 상자 실패와 정상 완료 선택의 실제 수치 기록
            if (party.GetLivingAllyCount() == 0) // 마지막 생존자 해체 비용 사망 확인
            { // 전멸 처리 시작
                session.CompleteExplorationFailure("수레를 해체하던 마지막 생존자가 쓰러졌습니다."); // 기존 전멸과 획득분 손실 처리
            } // 전멸 처리 종료
            return true; // 확정된 행동 반환
        } // 확정 행동 종료
        finally // 중복 처리 잠금 정리
        { // 잠금 해제 시작
            resolving = false; // 다음 방 선택 허용
        } // 잠금 해제 종료
    } // 행동 처리 종료

    internal static IEnumerator ResumeAmbush() // 결말 확인 또는 저장 복원 후 같은 매복 시작
    { // 매복 진입 시작
        var session = ExplorationSessionManager.Instance; // 현재 복원된 탐사 조회
        string room = session?.Cart.AmbushRoom; // 재개할 이벤트 방 고정
        while (!string.IsNullOrEmpty(room) && session != null && session.Cart.AmbushRoom == room && !session.IsExplorationCompleted && session.ActiveEncounter == null && SceneManager.GetActiveScene().name == "30_Exploration") // 동일한 미완료 매복만 재개
        { // 전투 전환 준비 대기 시작
            var player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 현재 탐사 플레이어 조회
            var flow = SceneFlowManager.Instance; // 정상 전투 전환 경로 조회
            if (player != null && flow != null && !flow.IsLoadingScene && !ExplorationPlayerController.InputBlocked) // 로딩과 결과 화면 입력 해제 확인
            { // 전투 시작 처리 시작
                var normal = FindNormal(session.Cart.NormalEncounter, session.EncounterPoolVersion); // 저장한 일반 적 편성 복원
                if (normal != null && Application.CanStreamedLevelBeLoaded("40_Battle") && session.BeginEncounter(session.Cart.AmbushBattleId, normal, player.transform.position, player.transform.position)) // 분리된 매복 조우 한 번 시작
                { // 실제 전투 씬 진입 시작
                    flow.LoadScene("40_Battle"); // 원래 전투 흐름에 매복 편성 전달
                } // 실제 전투 씬 진입 종료
                yield break; // 중복 진입 방지
            } // 전투 시작 처리 종료
            yield return null; // 다음 탐사 프레임 대기
        } // 전투 전환 준비 대기 종료
    } // 매복 진입 종료
} // 수레 이벤트 종료
