using System.Collections; // 실제 테스트 모드 변환과 카드 연출 대기
using NUnit.Framework; // 현재 행동력과 실제 할인 및 카드 사용 확인
using UnityEngine; // 독립적인 가변 원본과 연출 시간 기록
using UnityEngine.TestTools; // 원래 손패가 있는 플레이 모드 진입
using Object = UnityEngine.Object; // 한 번 만든 대조 원본만 해제
using Fight = WinterfallSwordCombatTests.Fight; // 기존 실제 전투 입력과 카드 목록 연결

public sealed class WinterfallPentacleEconomyTests // 첫 사용과 다음 카드 및 다음 아군 턴 행동력 회귀
{ // 펜타클 자원 기능의 실제 검사 시작
    [UnityTest] // 소유자와 유효 비용 및 실제 카드 정산 검사
    public IEnumerator RuneRecordingSurvivesFreeVariableCanceledAndLaterTurnUntilPositiveBaseUse() // 긍정 비용의 실제 사용만 할인을 소비하는 경계
    { // 새 룬 기록 할인 검사 시작
        yield return new EnterPlayMode(); // 손패와 전투 상태를 쓰는 실제 모드 진입
        yield return VerifyRuneRecording(); // 가변과 무료 및 취소와 다음 턴의 원래 자원 경계 확인
        yield return new ExitPlayMode(); // 검사 전투를 해제한 뒤 편집 모드 복귀
    } // 새 룬 기록 할인 검사 종료

    [UnityTest] // 실제 다음 아군 턴에서만 지급되는 공용 행동력 검사
    public IEnumerator EmergencyFundsStacksCapacityOnlyUntilTheFollowingAllyTurnEnds() // 다음 실제 아군 턴의 최대치와 현재치 변경 검증
    { // 예비 자금의 실제 다음 턴 검사 시작
        yield return new EnterPlayMode(); // 실제 손패와 아군 및 적 턴 루틴 준비
        yield return VerifyEmergencyFunds(); // 두 번 예약의 누적 및 실제 일시 상한 회수 비교
        yield return new ExitPlayMode(); // 원래 웨이브와 전투 자원 해제 뒤 편집 모드 복귀
    } // 예비 자금의 실제 다음 턴 검사 종료

    [UnityTest] // 취소와 무료 실제 카드 및 엄격한 첫 사용 환급 검사
    public IEnumerator SeizeOpportunityRefundsOnlyWhenItIsItselfTheFirstActualCard() // 처음 취소와 다음 진짜 카드가 다른 경계
    { // 기회 포착의 실제 턴별 사용 검사 시작
        yield return new EnterPlayMode(); // 공용 행동력 및 손패의 정상 사용 장면 진입
        yield return VerifyFirstUse(); // 취소와 반복 및 무료 실제 첫 사용의 결과 확인
        yield return new ExitPlayMode(); // 독립 검사 전투 정리 뒤 편집 모드 복귀
    } // 기회 포착의 실제 턴별 사용 검사 종료

    private static IEnumerator VerifyRuneRecording() // 실제 손패와 소유자 사이의 남아 있는 룬 기록 할인
    { // 유효한 한 번 기록의 실카드 검사 시작
        using var fight = new Fight("RUNE_RECORDING", "CHARGE", "GOLDEN_COIN", "MANA_RELEASE"); // 실제 가변 및 유료와 무료 카드 조합
        var attack = fight.View("CHARGE").RuntimeCard; // 사용을 미루는 실제 양수 기본 비용 카드
        var other = fight.Allies[1]; // 등록된 실제 할인 소유자와 다른 아군
        int baseline = attack.ApCost; // 기존 원래 고정 행동력 비용 기준
        int health = fight.Allies[0].CurrentHealth; // 지원 카드 사용 전 소유자 체력
        yield return fight.Use("RUNE_RECORDING"); // 실제로 쓴 첫 룬 기록의 일반 비용과 원본 손패 이동
        Assert.AreEqual(health, fight.Allies[0].CurrentHealth); // 룬 기록 사용 후 소유자 체력 유지 확인
        Assert.AreEqual(baseline - 1, attack.ApCost); // 같은 소유자의 실제 다음 유료 카드 비용 표시 확인
        var differentOwner = CardInstance.Create(attack.SourceData, other, 0, 0); // 유일 할인 주체를 대조할 동일 원본의 다른 실제 개별 카드
        Assert.AreEqual(baseline, differentOwner.ApCost); // 비시전자의 다른 카드에 예약 할인이 전달되지 않음을 확인
        VerifyAdditionalOnlyCost(fight.Allies[0]); // 고정 기본 비용이 없는 필수 추가 비용의 보존 확인
        fight.View("GOLDEN_COIN").OnPointerClick(null); // 실제 무료 카드 한 장 사용
        yield return Wait(fight); // 원래 무료 카드 및 실제 이동의 정산 대기
        Assert.AreEqual(baseline - 1, attack.ApCost); // 무료 카드는 다음 유료 카드 할인을 소모하지 않음을 확인
        fight.View("CHARGE").OnPointerClick(null); // 유효한 일반 유료 카드를 지불 전에 선택만 함
        fight.Controller.CancelSelection(); // 실제 다른 비용 지불 없이 타격 선택을 취소
        Assert.AreEqual(baseline - 1, attack.ApCost); // 취소된 유료 선택 뒤 할인 유지 확인
        var variable = fight.View("MANA_RELEASE").RuntimeCard; // 실제 양수 행동력을 모두 쓰는 가변 원본
        Assert.AreEqual("X", variable.CostLabel); // 현재 예약과 독립인 원래 가변 기호 유지 확인
        fight.View("MANA_RELEASE").OnPointerClick(null); // 가변 원본의 실제 공격 대상 선택 시작
        fight.EnemyViews[0].OnPointerClick(null); // 행동력 전액의 실제 공격 사용 확정
        yield return Wait(fight); // 원래 카드 소모와 실제 피해 정산의 완료
        Assert.Zero(fight.Points.CurrentActionPoints); // 가변 비용이 할인 없이 실제 현재 행동력 전액을 지불
        Assert.AreEqual(baseline - 1, attack.ApCost); // 가변 사용에 룬 기록 할인이 소모되지 않음 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 턴 종료의 다른 할인 초기화와 실제 손패 정리
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 턴에서 동일 개별 카드 다시 드로우
        Assert.AreEqual(baseline - 1, attack.ApCost); // 다른 턴에도 적법한 사용 전의 할인 계속 유지
        yield return fight.Use("CHARGE", 1); // 실제 살아 있는 적을 골라 할인된 유료 카드 사용 확정
        Assert.AreEqual(baseline, attack.ApCost); // 실제 한 번 정산 뒤 다음 정상 비용 복원
        Assert.AreEqual(20 - (baseline - 1), fight.Points.CurrentActionPoints); // 원래 실제 다음 아군 턴에서 줄어든 비용 확인
    } // 유효한 한 번 기록의 실카드 검사 종료

    private static void VerifyAdditionalOnlyCost(BattleUnitRuntime owner) // 실제 증가가 기본에만 적용되고 고정 추가 쪽은 유지되는지 확인
    { // 필수 추가 비용 자료 구성 시작
        var data = ScriptableObject.CreateInstance<CardData>(); // 기본 영과 별도 행동력 부담을 갖는 검사 원본
        try // 임시 카드 자료의 확정 정리 보장
        { // 원래 재화 분리 확인 시작
            JsonUtility.FromJsonOverwrite("{\"apCost\":0,\"additionalActionPointCost\":2}", data); // 고정 기본 영과 별도 필수 비용 둘을 원래 카드에 설정
            var extraOnly = CardInstance.Create(data, owner, 0, 0); // 같은 실제 할인 시전자가 가진 대조 카드
            Assert.AreEqual(2, extraOnly.ApCost); // 할인으로 원래 필수 행동력은 줄지 않음 확인
            Assert.Zero(extraOnly.EffectiveRuneRecordingDiscount); // 별도 비용만 있는 대상은 할인 소모 자격 없음 확인
        } // 원래 재화 분리 확인 종료
        finally // 테스트 전용 자료를 반드시 해제
        { // 임시 원본 정리 시작
            Object.Destroy(data); // 실제 게임 카드와 별개로 만든 대조 자료만 해제
        } // 임시 원본 정리 종료
    } // 필수 추가 비용 자료 구성 종료

    private static IEnumerator VerifyEmergencyFunds() // 다음 실제 아군 턴의 일시 최대치와 현재치
    { // 두 번 예약의 원래 전투 사용 검증 시작
        using var fight = new Fight("EMERGENCY_FUNDS", "EMERGENCY_FUNDS"); // 같은 원본의 별개 개별 예약 카드 두 장
        int health = fight.Allies[0].CurrentHealth; // 비상금 사용 전 소유자 체력
        yield return fight.Use("EMERGENCY_FUNDS"); // 실제 첫 행동력 예약 원본 사용
        yield return fight.Use("EMERGENCY_FUNDS"); // 두 번째 실제 원본 사용으로 별도 예약 누적
        Assert.AreEqual(20, fight.Points.MaxActionPoints); // 직전 두 사용이 현재 턴 상한을 바꾸지 않음 확인
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 무료 예약은 현재 행동력도 증가시키지 않음 확인
        Assert.AreEqual(health, fight.Allies[0].CurrentHealth); // 비상금 두 장 사용 후 소유자 체력 유지 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 실제 턴 종료 뒤 적 진영으로 전환
        Assert.AreEqual(20, fight.Points.MaxActionPoints); // 적 턴에는 추가 상승분이 적용되지 않음 확인
        Assert.Zero(fight.Points.CurrentActionPoints); // 적 턴의 실제 행동력 미사용 상태 유지 확인
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 다음 실제 아군 시작과 원래 회복 처리
        Assert.AreEqual(22, fight.Points.MaxActionPoints); // 실제 두 독립 예약만큼 추가된 아군의 이번 상한
        Assert.AreEqual(22, fight.Points.CurrentActionPoints); // 새로운 최대치와 함께 실제로 받은 현재 행동력
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 보상을 사용한 실제 아군 턴 종료
        Assert.AreEqual(20, fight.Points.MaxActionPoints); // 이후 정상 적 턴에서 회수된 이전 상한
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 새로운 예약 없이 세 번째 실제 아군 턴 준비
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 완료된 두 보상의 이중 지급 없음 확인
    } // 두 번 예약의 원래 전투 사용 검증 종료

    private static IEnumerator VerifyFirstUse() // 한 턴의 직접 결제와 실제 성공 사용 순서 검증
    { // 첫 카드와 무료 카드 및 취소 판정 검사 시작
        using var fight = new Fight("SEIZE_OPPORTUNITY", "SEIZE_OPPORTUNITY", "CHARGE", "GOLDEN_COIN"); // 같은 턴 반복과 무료 원본 카드를 가진 실제 손패
        fight.Allies[0].GainBlock(3); // 지원 효과 사용 전 소유자 방어도 준비
        fight.View("CHARGE").OnPointerClick(null); // 첫 실제 카드가 되기 전 지정 대상 대기
        fight.Controller.CancelSelection(); // 첫 카드 사용으로 집계되지 않는 원래 취소 입력
        fight.View("SEIZE_OPPORTUNITY").OnPointerClick(null); // 실제 첫 카드로 성공해 한 점을 돌려받는 원본
        yield return Wait(fight); // 발생하는 실제 원본 행동력 환급과 손패 이동 완료
        Assert.AreEqual(19, fight.Points.CurrentActionPoints); // 첫 원본 비용 둘과 별도의 한 점 환급 결과
        fight.View("SEIZE_OPPORTUNITY").OnPointerClick(null); // 같은 턴의 첫 번째가 아닌 두 번째 개별 원본 사용
        yield return Wait(fight); // 실제 사용 뒤 AP를 별도로 확인할 정산 대기
        Assert.AreEqual(17, fight.Points.CurrentActionPoints); // 같은 턴 두 번째 기회 포착은 일반 비용 전부 지불
        Assert.AreEqual(3, fight.Allies[0].CurrentBlock); // 기회 포착 두 장 사용 후 원래 방어도 유지 확인
        Assert.IsTrue(fight.Turns.EndPlayerTurn()); // 이미 사용을 마친 현재 실제 아군 턴 종료
        Assert.IsTrue(fight.Turns.CompleteEnemyTurn()); // 첫 사용을 다시 결정하는 새로운 아군 턴 도달
        fight.View("GOLDEN_COIN").OnPointerClick(null); // 무료이며 최대치로 환급량이 없어도 실제 원본 사용
        yield return Wait(fight); // 원래 무료 카드가 실제 손패를 떠날 때까지 대기
        Assert.AreEqual(20, fight.Points.CurrentActionPoints); // 오버플로우 없이 무료 사용을 끝낸 원래 최대치
        fight.View("SEIZE_OPPORTUNITY").OnPointerClick(null); // 무료 사용 뒤 이 아군 턴의 두 번째 원본
        yield return Wait(fight); // 실제 정상 비용 및 카드 이동 완료 대기
        Assert.AreEqual(18, fight.Points.CurrentActionPoints); // 무료 카드도 먼저 사용했으므로 환급 없음 확인
    } // 첫 카드와 무료 카드 및 취소 판정 검사 종료

    private static IEnumerator Wait(Fight fight) // 원래 카드 액션과 충돌 및 손패의 실제 이동 대기
    { // 실제 카드 사용의 완료 검증 시작
        float deadline = Time.realtimeSinceStartup + 12f; // 일반 손패 사용과 애니메이션 완료 경계
        while (fight.Runner.IsBusy && Time.realtimeSinceStartup < deadline) // 실제 지원 및 피해 연출의 종료까지 확인
        { // 다음 화면 프레임 대기 시작
            yield return null; // 원래 플레이 화면과 실제 자원의 정산 진행
        } // 다음 화면 프레임 대기 종료
        Assert.IsFalse(fight.Runner.IsBusy); // 정산을 끝낸 실제 전투 카드 연출 확인
    } // 실제 카드 사용의 완료 검증 종료
} // 펜타클 자원 기능의 실제 검사 종료
