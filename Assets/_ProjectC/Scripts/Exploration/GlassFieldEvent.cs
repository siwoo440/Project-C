using System; // 전투 효과 조합 표시
using System.Collections.Generic; // 선택지와 아군 목록 사용
using UnityEngine; // 이벤트 원본과 삽화 사용

[Flags] // 함께 보관할 수 있는 전투 효과
public enum GlassFieldEffect // 다음 실제 전투를 위한 들판 효과
{ // 효과 목록 시작
    None = 0, // 대기 효과 없음
    Evasion = 1, // 아군 전체 회피
    Blind = 2 // 아군 전체 실명
} // 효과 목록 종료

public static class GlassFieldEvent // 윈터폴 유리화된 들판의 선택과 전투 효과
{ // 이벤트 시작
    public const string Id = "winterfall_glass_field"; // 고정 이벤트 식별자
    public const int BreakGlass = 0; // 유리 속 카드 꺼내기
    public const int CrossCarefully = 1; // 조심히 지나가기
    public const int RunBlind = 2; // 눈을 감고 달리기
    internal static int GetBreakGlassHealthDamage(ExplorationRegion region) => BattleResultManager.GetEventTrapHealthDamage(6, region); // 현재 실제 지역의 함정 체력 피해
    private static bool resolving; // 지급 중 재입력 방지

    internal static ExplorationEventData Create() // 기획 원문과 선택지 생성
    { // 원본 생성 시작
        var choices = new List<ExplorationEventChoiceData> // 원문 순서의 선택지
        { // 결말 목록 시작
            Choice("유리를 깨서 꺼낸다", "유리가 깨지며 날카로운 조각이 팔을 긁는다.\n피가 떨어지자 유리 속 그림자가 잠깐 웃는다.\n손을 뻗어 꺼낸 것은 카드인지, 조각난 기억인지 구분이 안 된다.\n다만 당신의 손에 남았다."), // 유리를 깬 결말
            Choice("조심히 지나간다", "발을 디딜 때마다 유리 속 그림자들이 따라 움직인다.\n당신은 숨을 죽이고, 그들의 시선이 닿지 않는 각도를 찾으며 통과한다.\n익숙한 공포가 몸을 가볍게 만든다."), // 조심히 지나간 결말
            Choice("눈을 감고 달린다", "달리는 동안 유리가 연속으로 깨진다.\n시야를 닫았는데도, 머릿속에 빛이 번쩍인다.\n눈을 뜨면 안개가 더 짙어져 있고, 당신은 잠깐 여기가 어디인지 잊는다.") // 눈을 감고 달린 결말
        }; // 결말 목록 종료
        var data = ScriptableObject.CreateInstance<ExplorationEventData>(); // 탐사 선택 이벤트 생성
        data.Initialize(Id, "유리화된 들판", "한 구역의 땅은 재가 아니라 유리처럼 굳어 있다.\n발밑에서 잘그락 소리가 난다.\n유리 속에는 수많은 그림자가 갇혀 있고, 그중 하나가 당신을 향해 손을 뻗는다.", ExplorationEventCategory.Choice, Resources.Load<Sprite>("Exploration/GlassField"), choices); // 전용 삽화와 본문 연결
        return data; // 이벤트 원본 반환
    } // 원본 생성 종료

    private static ExplorationEventChoiceData Choice(string label, string story) // 선택 이름과 결말 연결
    { // 선택 생성 시작
        var choice = new ExplorationEventChoiceData(); // 선택 정보 생성
        choice.Initialize(label, story, null); // 행동과 결말 저장
        return choice; // 선택 정보 반환
    } // 선택 생성 종료

    public static bool TryResolve(string runtimeId, int choice, CharacterData actor, out string summary, ExplorationRegion region = ExplorationRegion.Winterfall) // 들판 선택 한 번 확정
    { // 선택 처리 시작
        summary = string.Empty; // 처리 전 안내 초기화
        var session = ExplorationSessionManager.EnsureInstance(); // 진행 중인 탐사 조회
        var allies = BattleResultManager.EnsureInstance(); // 출전 파티 상태 조회
        if (resolving || string.IsNullOrWhiteSpace(runtimeId) || session.ActiveEncounter != null || session.IsExplorationCompleted || session.IsEventResolved(runtimeId) || choice < BreakGlass || choice > RunBlind || allies.GetLivingAllyCount() < 1) // 확정 가능한 방과 생존자 확인
        { // 잘못된 선택 처리 시작
            return false; // 비용과 보상 보존
        } // 잘못된 선택 처리 종료
        resolving = true; // 지급 중 추가 선택 차단
        try // 행동 정산 보장
        { // 행동 결과 처리 시작
            bool successful = choice != RunBlind; // 들판의 정상 횡단과 실명 악화 결과의 구분
            if (choice == BreakGlass) // 행동자 소유 카드 지급 확인
            { // 유리 깨기 시작
                if (!allies.IsLivingPartyMember(actor) || !allies.TryGetSavedAllyState(actor, out int health, out _, out _) || !WinterfallRiskCardReward.TryGrant(runtimeId, actor, out CardData reward)) // 생존 행동자 검증과 카드 지급
                { // 카드 미지급 처리 시작
                    summary = "생존 파티원에게 카드를 지급할 수 없습니다."; // 다른 선택 안내
                    return false; // 비용과 방 유지
                } // 카드 미지급 처리 종료
                allies.ApplyEventChange(actor, GetBreakGlassHealthDamage(region), 0); // 유리 파편의 보정된 체력 피해 적용
                allies.TryGetSavedAllyState(actor, out int afterHealth, out _, out _); // 실제 유리 피해 뒤 남은 체력 조회
                successful = reward.CardId != WinterfallRiskCardReward.CurseCardId; // 실제 희귀 카드 당첨과 저주 실패 결과 구분
                summary = $"{actor.DisplayName} 체력 {health} → {afterHealth} · [{reward.DisplayName}] 획득"; // 실제 남은 체력과 획득 카드 표시
            } // 유리 깨기 종료
            else // 다음 전투 효과 선택
            { // 효과 보관 시작
                bool careful = choice == CrossCarefully; // 회피와 실명 구분
                session.PendingGlassFieldEffects |= careful ? GlassFieldEffect.Evasion : GlassFieldEffect.Blind; // 같은 효과 중복 없이 두 선택 보관
                summary = careful ? "다음 전투 아군 전체 회피 +10%p · 2턴" : "다음 전투 아군 전체 실명 · 명중률 −15%p · 1턴"; // 다음 전투의 대상과 기간 안내
            } // 효과 보관 종료
            session.MarkEventResolved(runtimeId, successful); // 실제 들판 선택 결과의 구역 수치와 방 완료를 기록
            if (choice == BreakGlass && allies.IsActivePartyWiped()) // 마지막 생존자 사망 확인
            { // 전멸 처리 시작
                session.CompleteExplorationFailure("유리화된 들판에서 마지막 생존자가 쓰러졌습니다."); // 기존 탐사 실패와 자원 정산
            } // 전멸 처리 종료
            return true; // 방 정산 완료
        } // 행동 결과 처리 종료
        finally // 지급 잠금 해제
        { // 입력 복구 시작
            resolving = false; // 다음 방 입력 허용
        } // 입력 복구 종료
    } // 선택 처리 종료

    public static void ApplyBattleStart(ExplorationSessionManager session, IReadOnlyList<BattleUnitRuntime> allies) // 실제 교전의 첫 아군 턴에 대기 효과 소비
    { // 전투 시작 효과 처리 시작
        if (session == null || session.ActiveEncounter == null || session.PendingGlassFieldEffects == GlassFieldEffect.None || allies == null) // 탐사에서 진입한 교전과 효과 확인
        { // 대기 유지 시작
            return; // 실제 교전 전 효과 보존
        } // 대기 유지 종료
        GlassFieldEffect effects = session.PendingGlassFieldEffects; // 현재 교전에 사용할 효과 복사
        for (int index = 0; index < allies.Count; index++) // 출전한 아군 순회
        { // 아군별 적용 시작
            BattleUnitRuntime ally = allies[index]; // 현재 전투 유닛 조회
            if (ally == null || ally.IsDead || ally.Team != BattleTeam.Ally) // 생존 아군 확인
            { // 대상 제외 시작
                continue; // 다음 출전자 확인
            } // 대상 제외 종료
            session.PendingGlassFieldEffects = GlassFieldEffect.None; // 첫 생존 아군 확인 뒤 같은 교전 재적용 차단
            if ((effects & GlassFieldEffect.Evasion) != 0) // 조심히 지나간 선택 확인
            { // 회피 적용 시작
                ally.ApplyStatusEffect(BattleStatusEffectType.Evasion, 10, 2, 1); // 적 턴 두 번 동안 회피 십 퍼센트포인트 부여
            } // 회피 적용 종료
            if ((effects & GlassFieldEffect.Blind) != 0) // 눈을 감고 달린 선택 확인
            { // 실명 적용 시작
                ally.ApplyStatusEffect(BattleStatusEffectType.Blind, 15, 1, 1); // 첫 아군 턴 명중률 십오 퍼센트포인트 감소
                ally.GetStatusEffect(BattleStatusEffectType.Blind)?.ConsumeDurationDecreaseSkip(); // 첫 아군 턴 종료를 실명 한 턴으로 계산
            } // 실명 적용 종료
        } // 아군별 적용 종료
    } // 전투 시작 효과 처리 종료
} // 이벤트 종료
