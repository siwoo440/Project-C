public enum BattleTurnPhase // 전투 단계 종류
{
    BattleSetup, // 전투 준비
    PlayerTurn, // 플레이어 턴
    EnemyIntent, // 적 행동 예고
    EnemyAction, // 적 행동 실행
    StatusProcessing, // 상태 효과 처리
    BattleEnded // 전투 종료
}