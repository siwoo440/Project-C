using System; // 시드 난수 사용
using System.Linq; // 보유 원본과 사건 검색
using UnityEngine; // 탐사 좌표와 저장 필드 사용

[Serializable] // 회차 저장 대상
public sealed class CrackedCompassRunState // 첫 일반 방의 나침반 판정 기록
{ // 나침반 기록 시작
    private const string RelicId = "RLC_CRACKED_COMPASS"; // 실제 나침반 식별자
    [SerializeField] private bool pending; // 출발 때 보유한 미사용 기회
    [SerializeField] private int foundSeed; // 발견한 지도의 시드
    [SerializeField] private Vector2Int foundRoom; // 발견한 일반 방의 좌표
    [SerializeField] private string eventId; // 최초 추첨한 사건 원본
    [NonSerialized] private string cachedEncounterId; // 매 프레임 재사용할 교체 전투 식별자
    public bool Pending => pending; // 첫 일반 방 대기 여부
    public Vector2Int FoundRoom => foundRoom; // 발견 위치 조회
    public string EventId => eventId; // 저장된 사건 조회
    public string EncounterId => string.IsNullOrEmpty(eventId) ? null : (cachedEncounterId ??= $"F1_X{foundRoom.x}_Y{foundRoom.y}_S{foundSeed}"); // 최초 사용 시 복원하고 같은 표식 비교에 재사용

    public static CrackedCompassRunState ForDeparture(RelicInventoryRuntime inventory) // 출발 당시의 보유 상태 보관
    { // 출발 판정 시작
        return new CrackedCompassRunState // 출발 때의 기회 생성
        { // 출발 상태 설정 시작
            pending = inventory != null && inventory.ContainsRelic(RelicId) // 실제 보유할 때만 기회 부여
        }; // 출발 상태 설정 종료
    } // 출발 판정 종료

    public bool Visit(int floor, int seed, ExplorationMapCell cell, int eventPoolVersion, RelicInventoryRuntime inventory, ExpeditionZoneId zone = ExpeditionZoneId.WinterfallAshPlains) // 첫 일반 방에서 실제 출정 지역의 발견 기회 소모
    { // 방문 판정 시작
        if (!pending || floor != 1 || cell == null || cell.Type != ExplorationCellType.Normal || cell.RoomType != ExplorationRoomType.Normal) // 출발 방과 특수 방 및 재방문 제외
        { // 일반 방 밖의 처리 시작
            return false; // 미사용 기회 유지
        } // 일반 방 밖의 처리 종료
        pending = false; // 성공과 실패 모두 첫 방문으로 고정
        RelicData compass = inventory?.OwnedRelics.FirstOrDefault(relic => relic != null && relic.RelicId == RelicId); // 방문 당시의 실제 보유 나침반
        var random = new System.Random(seed ^ 1370495623); // 기존 방 배치와 독립된 층 시드
        if (compass != null && random.Next(100) < compass.EffectValue) // 원본의 추가 발견 확률 판정
        { // 발견 성공 처리 시작
            var candidates = ExplorationEventCatalog.LoadZoneEvents(zone, eventPoolVersion).Where(item => item != null && item.IsValidData()).OrderBy(item => item.EventId, StringComparer.Ordinal).ToArray(); // 현재 층 사건의 고정 후보 순서
            if (candidates.Length > 0) // 배치 가능한 실제 사건 확인
            { // 사건 보관 시작
                eventId = candidates[random.Next(candidates.Length)].EventId; // 선택한 사건 원본 보관
                foundRoom = cell.Coordinate; // 최초 방문 위치 고정
                foundSeed = seed; // 발견한 지도 고정
            } // 사건 보관 종료
        } // 발견 성공 처리 종료
        return true; // 첫 일반 방의 판정 완료
    } // 방문 판정 종료

    public bool MatchesMap(int floor, int seed) // 발견한 원래 지도 확인
    { // 지도 확인 시작
        return floor == 1 && seed == foundSeed && !string.IsNullOrEmpty(eventId); // 같은 일층 지도에서만 사건 복원
    } // 지도 확인 종료

    public bool IsValid(int floor, int eventPoolVersion, ExpeditionZoneId zone = ExpeditionZoneId.WinterfallAshPlains) // 저장된 상태와 해당 지역의 사건 원본 확인
    { // 저장 검사 시작
        if (string.IsNullOrEmpty(eventId)) // 발견 전 또는 실패한 상태 확인
        { // 사건 없는 저장 확인 시작
            return !pending || floor == 1; // 첫 층에서만 미사용 기회 허용
        } // 사건 없는 저장 확인 종료
        return !pending && floor == 1 && ExplorationEventCatalog.LoadZoneEvents(zone, eventPoolVersion).Any(item => item != null && item.EventId == eventId); // 소모 상태와 현재 목록의 사건 확인
    } // 저장 검사 종료
} // 나침반 기록 종료
