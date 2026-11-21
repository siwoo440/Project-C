using System.Collections.Generic; // 탐사 선택지 목록
using System.Linq; // 실제 회복 아이템 조회
using UnityEngine; // 기획 원본과 전용 삽화 사용

public static class AshWandererCryEvent // 윈터폴 잿빛 방랑자의 울음 선택 처리
{ // 사건 시작
    public const string Id = "winterfall_ash_wanderer_cry"; // 기획 사건의 고정 식별자
    public const int Follow = 0; // 목소리를 따라가는 선택
    public const int CoverEars = 1; // 귀를 막고 이동하는 선택
    public const int Pray = 2; // 짧게 기도하는 선택
    private static bool resolving; // 행동 확정 중 재입력 잠금

    internal static ExplorationEventData Create() // 원문 본문과 세 가지 결말 생성
    { // 원본 생성 시작
        var choices = new List<ExplorationEventChoiceData> // 기획 순서의 선택지 목록
        { // 선택지 구성 시작
            Choice("목소리를 따라간다", "울음을 따라가자 안개가 갈라지고, 그곳에는 아무도 없다.\n대신 재 위에 당신의 발자국이 먼저 찍혀 있다.\n당신은 본능적으로 뒤를 돌아보지만, 이미 돌아갈 길이 옅어졌다."), // 목소리를 따라간 결말
            Choice("귀를 막고 이동한다", "귀를 막자 울음은 작아지지 않는다.\n손바닥 아래에서 울린다.\n잠깐의 방향 감각이 무너지고, 재 안개 속에서 무언가가 먼저 다가오는 소리가 들린다."), // 귀를 막은 결말
            Choice("짧게 기도한다", "기도는 신에게 닿지 않는다.\n대신, 당신 스스로에게 닿는다.\n울음이 잠시 멀어지고, 주머니 속에서 재에 젖지 않은 작은 병이 굴러 나온다.") // 짧게 기도한 결말
        }; // 선택지 구성 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 실제 탐사 이벤트 원본 생성
        data.Initialize(Id, "잿빛 방랑자의 울음", "재 안개 너머로 누군가 울고 있다.\n사람의 울음인지, 바람의 소리인지 구분이 안 된다.\n한 걸음 다가갈 때마다 울음은 또렷해지고, 동시에 당신 이름을 부르는 것 같다.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/AshWandererCry"), choices); // 원문과 전용 삽화 연결
        return data; // 이벤트 원본 반환
    } // 원본 생성 종료

    private static ExplorationEventChoiceData Choice(string label, string story) // 원문 선택과 결말 연결
    { // 선택 생성 시작
        var choice = new ExplorationEventChoiceData(); // 선택 원본 생성
        choice.Initialize(label, story, null); // 선택 문구와 결말 저장
        return choice; // 완성된 선택 반환
    } // 선택 생성 종료

    public static bool TryResolve(string room, int choice, CharacterData actor, out string summary) // 선택한 방 한 번 확정
    { // 선택 처리 시작
        summary = string.Empty; // 처리 전 문구 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 실 탐사 상태 조회
        var party = BattleResultManager.EnsureInstance(); // 파티 생존 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(room) || session.IsExplorationCompleted || session.ActiveEncounter != null || session.IsEventResolved(room) || choice < Follow || choice > Pray || party.GetLivingAllyCount() < 1 || choice != CoverEars && !party.IsLivingPartyMember(actor)) // 미처리 방과 생존 행동자 확인
        { // 잘못된 선택 처리 시작
            return false; // 비용과 대기 효과 보존
        } // 잘못된 선택 처리 종료
        resolving = true; // 중복 클릭 차단
        try // 입력 잠금 복구 보장
        { // 선택 결과 처리 시작
            bool successful = true; // 확률 기도를 제외한 정상 선택의 기존 완료
            if (choice == CoverEars) // 대상 없는 이동 선택 확인
            { // 귀 막기 시작
                session.WandererCry.CoverEars(); // 다음 전투 전원 명중률 감소 예약
                summary = "다음 전투 첫 아군 턴 · 전원 명중률 −10%p"; // 전투 대상과 기간 안내
            } // 귀 막기 종료
            else // 대상이 있는 선택 처리
            { // 정신력 변화 시작
                party.TryGetSavedAllyState(actor, out _, out int mental, out _); // 행동 전 정신력 조회
                if (choice == Follow) // 목소리 추적 선택 확인
                { // 추적 처리 시작
                    if (!session.WandererCry.TryFollow(room)) // 보관 중인 추적과 실제 후보 확보 확인
                    { // 추적 불가 처리 시작
                        summary = session.WandererCry.HasFollow ? "이미 목소리를 따라가고 있습니다. 다른 행동을 선택하세요." : "승리 보상으로 줄 공용 카드 세 장을 준비할 수 없습니다."; // 기존 추적 또는 보상 부족 사유 안내
                        return false; // 정신력과 이번 방 보존
                    } // 추적 불가 처리 종료
                    party.ApplyEventChange(actor, 0, -5); // 선택한 생존자 정신력 다섯 감소
                    summary = $"{actor.DisplayName} 정신력 {mental} → {Mathf.Max(0, mental - 5)} · 다음 적 선제 +15%p · 승리 시 공용 카드 1장 선택"; // 비용과 위험 및 승리 보상 안내
                } // 추적 처리 종료
                else // 짧은 기도 선택
                { // 기도 처리 시작
                    bool found = WandererCryRunState.Roll(room, "prayer") < 30; // 같은 방의 회복 아이템 삼십 퍼센트 판정
                    successful = found; // 회복 물품의 실제 당첨 여부와 같은 사건 분기
                    string reward = "회복 아이템 없음"; // 미당첨 시 실제 결과
                    if (found) // 회복 아이템 당첨 확인
                    { // 회복 보상 처리 시작
                        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 저장 가능 아이템 조회
                        var potion = catalog?.Consumables.OfType<PotionData>().Where(item => item.IsValidData() && item.EffectType == PotionEffectType.RestoreHealth).OrderBy(item => item.ItemId, System.StringComparer.Ordinal).FirstOrDefault(); // 기존 등록된 체력 회복 포션 선택
                        if (potion == null || !ConsumableRunManager.EnsureInstance().TryAcquire(potion, out _)) // 빈 칸과 회복 아이템 지급 확인
                        { // 지급 실패 처리 시작
                            summary = "회복 아이템을 보관할 수 없습니다. 보관함을 확인하세요."; // 다른 선택이나 보관함 정리 안내
                            return false; // 정신력과 당첨 방 보존
                        } // 지급 실패 처리 종료
                        reward = $"[{potion.DisplayName}] 1개 획득"; // 실제 지급된 아이템 안내
                    } // 회복 보상 처리 종료
                    party.ApplyEventChange(actor, 0, 2); // 선택한 생존자 정신력 두 점 회복
                    summary = $"{actor.DisplayName} 정신력 {mental} → {Mathf.Min(BattleMentalRuntime.MaximumMental, mental + 2)} · {reward}"; // 실제 정신력과 당첨 결과 안내
                } // 기도 처리 종료
            } // 정신력 변화 종료
            session.MarkEventResolved(room, successful); // 같은 방의 실제 기도 결과와 일반 선택 수치 기록
            return true; // 선택 결과 확정
        } // 선택 결과 처리 종료
        finally // 선택 입력 복구
        { // 잠금 해제 시작
            resolving = false; // 후속 방의 정상 선택 허용
        } // 잠금 해제 종료
    } // 선택 처리 종료
} // 사건 종료
