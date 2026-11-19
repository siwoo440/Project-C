using System.Collections.Generic; // 선택지와 유물 목록 사용
using System.Linq; // 저장 가능한 유물 원본 대조
using UnityEngine; // 이벤트 원본과 삽화 사용

public static class AshPileEvent // 윈터폴 잿더미 조사 이벤트
{ // 이벤트 시작
    public const string Id = "winterfall_ash_pile"; // 원본 이벤트 식별자
    public const int BareHands = 0; // 맨손 조사 선택 순서
    public const int UseTool = 1; // 도구 조사 선택 순서
    public const int Cover = 2; // 잿더미 덮기 선택 순서
    private const string BareStory = "손끝이 재 속으로 잠기자, 살갗이 천천히 갈라지는 느낌이 든다.\n따끔함이 아니라 기억이 벗겨지는 감각."; // 맨손 조사 도입
    private const string ToolStory = "삽날이 재를 파고들자 재가 끈적하게 들러붙는다.\n마치 파는 것이 아니라 살을 들어 올리는 것 같다."; // 도구 조사 도입
    private const string CoverStory = "다시 재를 덮자 빛은 금세 사라진다.\n이상하게도 마음이 가라앉는다.\n‘지금은… 모르는 게 낫다.’\n당신은 그렇게 스스로를 설득한다."; // 덮기 결말
    private static bool resolving; // 결과 반영 중 중복 입력 차단

    internal static ExplorationEventData Create() // 기획 원문과 세 선택지 연결
    { // 원본 생성 시작
        var choices = new List<ExplorationEventChoiceData>(); // 화면 선택지 준비
        string[] labels = { "맨손으로 뒤진다", "도구를 사용한다", "덮어둔다" }; // 원문 행동 순서
        for (int index = 0; index < labels.Length; index++) // 세 행동 순회
        { // 행동 등록 시작
            var choice = new ExplorationEventChoiceData(); // 행동 원본 생성
            choice.Initialize(labels[index], GetStory(index, true), null); // 행동 이름과 성공 결말 지정
            choices.Add(choice); // 이벤트 선택지 추가
        } // 행동 등록 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 원본 이벤트 생성
        data.Initialize(Id, "잿더미 속에서 빛나는 것", "바람이 잦아든 순간, 잿더미 한가운데서 금속 같은 빛이 번뜩인다.\n가까이 다가가자 재가 가라앉는 게 아니라… 마치 살아있는 것처럼 흐른다.\n손을 넣으면 따뜻하다. 불이 아닌데도, 심장이 가까이 있는 것처럼.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/AshPile"), choices); // 윈터폴 본문과 전용 삽화 적용
        return data; // 배치 가능한 이벤트 반환
    } // 원본 생성 종료

    private static string GetStory(int choice, bool found) // 행동과 추첨 결과에 맞는 원문 결말
    { // 원문 선택 시작
        if (choice == BareHands) // 맨손 조사 여부 확인
        { // 맨손 원문 선택 시작
            return found ? BareStory + "\n손을 뽑아 올렸을 때, 손바닥에는 재가 아니라 형태를 가진 무언가가 쥐어져 있다." : BareStory; // 획득한 경우에만 유물 발견 문장 표시
        } // 맨손 원문 선택 종료
        if (choice == UseTool) // 도구 조사 여부 확인
        { // 도구 원문 선택 시작
            return found ? ToolStory + "\n끝내 작은 물체 하나가 드러나고, 그것은 재에 젖지 않은 채 은은하게 빛난다." : ToolStory; // 획득한 경우에만 발견 문장 표시
        } // 도구 원문 선택 종료
        return CoverStory; // 잿더미를 덮는 결말 반환
    } // 원문 선택 종료

    public static bool TryResolve(string runtimeId, int choice, CharacterData actor, out string story, out string summary) // 행동 확정과 일회성 보상 지급
    { // 기본 행동 처리 시작
        return TryResolve(runtimeId, choice, actor, null, out story, out summary); // 단일 후보의 기존 조사와 덮기 처리
    } // 기본 행동 처리 종료
    public static bool TryResolve(string runtimeId, int choice, CharacterData actor, RelicData selected, out string story, out string summary) // 선택한 후보 검증과 일회성 지급
    { // 결과 처리 시작
        story = string.Empty; // 미확정 원문 초기화
        summary = string.Empty; // 미확정 결과 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 탐사 세션 조회
        var allies = BattleResultManager.EnsureInstance(); // 출전 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(runtimeId) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(runtimeId) || choice < BareHands || choice > Cover || allies.GetLivingAllyCount() < 1) // 현재 방과 탐사 진행 여부 검증
        { // 잘못된 요청 처리 시작
            return false; // 중복 처리와 종료된 탐사 차단
        } // 잘못된 요청 처리 종료
        if (choice != UseTool && !allies.IsLivingPartyMember(actor)) // 맨손과 덮기의 생존 행동자 확인
        { // 대상 검증 실패 시작
            return false; // 비용과 보상 유지
        } // 대상 검증 실패 종료
        resolving = true; // 상태 변경 알림 중 같은 방의 재진입 차단
        try // 확정 처리 후 잠금 정리
        { // 선택 결과 처리 시작
            bool found = false; // 유물 획득 여부 초기화
            if (choice == Cover) // 잿더미를 덮는 행동 확인
            { // 정신력 회복 시작
                allies.TryGetSavedAllyState(actor, out _, out int mental, out _); // 행동 전 정신력 조회
                allies.ApplyEventChange(actor, 0, 3); // 선택한 생존자 정신력 세 점 회복
                summary = $"{actor.DisplayName} 정신력 {mental} → {Mathf.Min(BattleMentalRuntime.MaximumMental, mental + 3)}"; // 정신력 상한을 반영한 결과 표시
            } // 정신력 회복 종료
            else // 맨손과 도구로 유물을 찾는 행동
            { // 확률 보상 처리 시작
                var resources = PlayerResourceManager.EnsureInstance(); // 현재 제작 자원 조회
                if (choice == UseTool && !resources.CanAfford(0, 0, 1, 0)) // 철판 한 개 보유 여부 확인
                { // 철판 부족 안내 시작
                    summary = "도구를 사용하려면 철판 1개가 필요합니다."; // 미확정 선택 안내
                    return false; // 다른 행동을 선택할 수 있도록 유지
                } // 철판 부족 안내 종료
                var options = GetRewardChoices(runtimeId, choice, out found); // 같은 방의 기존 추첨과 추가 후보 조회
                if (options.Count == 0) // 모든 지급 후보 소진 여부 확인
                { // 유물 없음 안내 시작
                    summary = "발견할 수 있는 새 유물이 없습니다. 잿더미를 덮을 수 있습니다."; // 비용 없는 선택 복귀 안내
                    return false; // 지급 후보 없는 조사 비용과 판정 보존
                } // 유물 없음 안내 종료
                if (selected != null && !options.Contains(selected) || selected == null && options.Count > 1) // 선택 누락 또는 이미 바뀐 후보 확인
                { // 잘못된 보상 선택 시작
                    summary = "현재 후보 중에서 받을 유물을 하나 선택하세요."; // 재선택 안내
                    return false; // 철판과 체력 및 방 완료 기록 보존
                } // 잘못된 보상 선택 종료
                RelicData candidate = selected != null ? selected : options[0]; // 지급 직전 실제로 선택한 후보 확정
                int temporaryIron = resources.ExplorationIronPlate; // 지급 실패 시 기존 철판과 탐사 획득분 구분 보존
                if (choice == UseTool && !resources.TrySpend(0, 0, 1, 0)) // 도구 비용 실제 지불 확인
                { // 자원 지불 실패 시작
                    return false; // 철판 미지불 상태의 보상 차단
                } // 자원 지불 실패 종료
                if (found) // 유물 추첨 성공 확인
                { // 유물 지급 시작
                    var relics = RelicRunManager.EnsureInstance(); // 실제 유물 보관함 조회
                    if (relics.Inventory.ContainsRelic(candidate.RelicId) || relics.TryAcquire(candidate) != RelicAcquireResult.Acquired) // 비용 변경 직후 실제 신규 유물 지급 확인
                    { // 지급 실패 복구 시작
                        if (choice == UseTool) // 이미 낸 철판 여부 확인
                        { // 철판 환불 시작
                            resources.AddResources(0, 0, 1, 0); // 도구 비용 한 개 반환
                            if (resources.IsTrackingExplorationResources) // 탐사 재화 추적 여부 확인
                            { // 기존 재화 구분 복구 시작
                                resources.RestoreExplorationResourceTracking(resources.ExplorationGold, resources.ExplorationScrew, temporaryIron, resources.ExplorationWire); // 환불 철판의 기존 소유 구분 복원
                            } // 기존 재화 구분 복구 종료
                        } // 철판 환불 종료
                        summary = "유물을 지급하지 못했습니다. 다시 선택하세요."; // 무상 재선택 안내
                        return false; // 미처리 방과 행동자 상태 보존
                    } // 지급 실패 복구 종료
                } // 유물 지급 종료
                if (choice == BareHands) // 맨손 체력 비용 확인
                { // 맨손 비용 반영 시작
                    allies.TryGetSavedAllyState(actor, out int health, out _, out _); // 피해 전 체력 조회
                    allies.ApplyEventChange(actor, 6, 0); // 선택한 생존자 체력 여섯 감소
                    summary = $"{actor.DisplayName} 체력 {health} → {Mathf.Max(0, health - 6)}"; // 실제 피해 표시
                } // 맨손 비용 반영 종료
                else // 철판 비용 결과
                { // 도구 비용 표시 시작
                    summary = "철판 −1"; // 도구 비용만 표시
                } // 도구 비용 표시 종료
                summary += found ? $" · 유물 [{candidate.DisplayName}] 획득" : " · 발견한 유물 없음"; // 실제 지급 여부 표시
            } // 확률 보상 처리 종료
            story = GetStory(choice, found); // 실제 결과와 일치하는 원문 선택
            session.MarkEventResolved(runtimeId); // 비용과 보상 확정 후 방 사용 기록
            if (choice == BareHands && allies.IsActivePartyWiped()) // 마지막 생존자의 맨손 조사 사망 여부 확인
            { // 전멸 정산 시작
                session.CompleteExplorationFailure("잿더미를 뒤지던 마지막 생존자가 쓰러졌습니다."); // 기존 쇠약·회복 및 탐사 실패 처리
            } // 전멸 정산 종료
            return true; // 행동 완료 반환
        } // 선택 결과 처리 종료
        finally // 다음 입력을 위한 잠금 정리
        { // 잠금 복구 시작
            resolving = false; // 선택 중 재진입 차단 해제
        } // 잠금 복구 종료
    } // 결과 처리 종료

    public static List<RelicData> GetRewardChoices(string runtimeId, int choice, out bool found) // 미보유 유물의 고정된 방별 선택 목록
    { // 유물 추첨 시작
        var result = new List<RelicData>(); // 실제 지급 가능한 선택 후보
        found = false; // 조사 성공 여부 초기화
        if (string.IsNullOrWhiteSpace(runtimeId) || choice != BareHands && choice != UseTool) // 유물을 찾는 유효한 방과 행동 확인
        { // 조사 대상 확인 시작
            return result; // 덮기와 잘못된 방에는 추첨 없음
        } // 조사 대상 확인 종료
        var random = new System.Random(ExplorationTreasureRewardService.BuildStableHash(runtimeId)); // 기존 방의 성공과 첫 후보 추첨 순서 유지
        found = random.Next(100) < (choice == BareHands ? 40 : 70); // 맨손 또는 도구 성공 추첨
        var sheet = RelicSheetCatalog.Load(); // 기획 시트 원본 조회
        var saved = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 저장 가능한 원본 조회
        if (sheet == null || saved == null) // 유물 원본 목록 확인
        { // 원본 누락 처리 시작
            return result; // 지급 가능한 원본 없음 반환
        } // 원본 누락 처리 종료
        var inventory = RelicRunManager.EnsureInstance().Inventory; // 현재 보유 유물 조회
        var candidates = new List<RelicData>(); // 실제 미보유 유물 목록
        foreach (var offer in ExplorationTreasureRewardService.GetValidOffers(ShopOfferType.Relic)) // 기존 보물 방의 유효 상품 순회
        { // 유물 후보 확인 시작
            var relic = offer.Relic; // 실제 유물 원본 조회
            if (saved.Relics.Contains(relic) && sheet.FindByName(relic.DisplayName) != null && !inventory.ContainsRelic(relic.RelicId) && !candidates.Contains(relic)) // 저장 가능한 시트 원본과 미보유 여부 확인
            { // 미보유 유물 추가 시작
                candidates.Add(relic); // 중복 없는 실제 보상 후보 추가
            } // 미보유 유물 추가 종료
        } // 유물 후보 확인 종료
        candidates.Sort((left, right) => string.CompareOrdinal(left.RelicId, right.RelicId)); // 같은 저장에서 보상 순서 고정
        if (candidates.Count > 0) // 첫 보물 후보 확인
        { // 후보 선택 시작
            int first = random.Next(candidates.Count); // 기존 단일 보상의 고정 순서 유지
            result.Add(candidates[first]); // 기본 보상 한 개 등록
            candidates.RemoveAt(first); // 같은 유물의 중복 선택 방지
            var tweezers = inventory.FindEffect(RelicEffectType.ExtraEventRelicChoice); // 탐사 중 실제 보유한 핀셋 확인
            if (found && tweezers != null && candidates.Count > 0 && random.Next(100) < tweezers.TriggerChancePercent) // 유물 발견 성공 후 삼십 퍼센트 추가 추첨
            { // 대안 유물 추가 시작
                result.Add(candidates[random.Next(candidates.Count)]); // 다른 미보유 유물 한 개 추가
            } // 대안 유물 추가 종료
        } // 후보 선택 종료
        return result; // 실제로 받을 수 있는 선택 후보 반환
    } // 유물 추첨 종료
} // 이벤트 종료
