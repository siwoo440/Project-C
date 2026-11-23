public enum CardTargetType
{
    Self, // 자신 대상
    SingleAlly, // 단일 아군 대상
    AllAllies, // 전체 아군 대상
    SingleEnemy, // 단일 적 대상
    AllEnemies, // 전체 적 대상
    FallenAlly, // 현재 전투에서 쓰러진 아군 한 명 대상
    LowestHealthAlly, // 현재 체력이 가장 낮은 생존 아군 자동 선택
    EnemyAndAlly, // 적과 아군을 차례로 하나씩 지정
    BlessingSourceAndAlly, // 강화 제공자와 받을 다른 아군을 차례로 지정
    RandomTwoEnemies, // 현재 생존 적 중 최대 두 명을 중복 없이 선택
    SelectedTwoEnemies // 플레이어가 서로 다른 생존 적을 최대 두 명 선택
}
