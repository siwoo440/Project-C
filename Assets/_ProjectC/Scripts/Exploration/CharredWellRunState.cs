using System; // 회차 상태 직렬화
using System.Collections.Generic; // 물 효과 보유자 목록
using System.Linq; // 저장 원본과 다른 이벤트 대조
using UnityEngine; // 탐사 상태 직렬화 필드

[Serializable] // 탐사 저장 파일에 포함할 우물 상태
public sealed class CharredWellRunState // 검게 탄 우물의 회복과 봉인 기록
{ // 우물 상태 시작
    [SerializeField] private List<string> waterOwners = new List<string>(); // 아직 회복하지 않은 행동자 식별자
    [SerializeField] private string nextSealRoom; // 다음 층에 발동할 봉인의 원래 방
    [SerializeField] private string currentSealRoom; // 현재 층 최초 재폭풍의 고정 판정 근거
    [SerializeField] private bool currentSealUsed; // 실제 배치에서 현재 층 봉인 판정 완료 여부
    [SerializeField] private int consumedMapSeed; // 봉인을 소모한 지도의 재생성 시드

    public bool HasNextSeal => !string.IsNullOrEmpty(nextSealRoom); // 다음 층에 대기 중인 봉인
    public bool CurrentSealUsed => currentSealUsed; // 성공과 실패를 포함한 봉인 소모 상태

    public bool HasWater(string ownerId) // 행동자에게 남은 회복 효과 확인
    { // 보유 확인 시작
        return waterOwners != null && waterOwners.Contains(ownerId); // 소유자 식별자가 일치하는 효과 확인
    } // 보유 확인 종료

    public bool TryReserveWater(string ownerId) // 행동자에게 미사용 회복 한 번 보관
    { // 물 보관 시작
        if (string.IsNullOrWhiteSpace(ownerId) || HasWater(ownerId)) // 행동자 누락과 중복 보관 확인
        { // 보관 불가 시작
            return false; // 기존 회복 효과 보존
        } // 보관 불가 종료
        waterOwners ??= new List<string>(); // 이전 저장의 빈 효과 목록 준비
        waterOwners.Add(ownerId); // 해당 행동자의 회복 예약
        return true; // 회복 한 번 보관 성공
    } // 물 보관 종료

    public int TryHealAfterDamage(BattleUnitRuntime target, int hpDamage) // 실제 피해를 받고 생존한 보유자 즉시 회복
    { // 자동 회복 시작
        if (hpDamage <= 0 || target == null || target.IsDead || target.CharacterSource == null || waterOwners == null || !waterOwners.Remove(target.CharacterSource.CharacterId)) // 실제 체력 피해와 생존 및 효과 소유 확인
        { // 발동 제외 시작
            return 0; // 다른 유닛과 전부 흡수한 피해 제외
        } // 발동 제외 종료
        return target.RestoreHealth(8, target); // 효과 소모 후 최대 체력 내 자기 회복 여덟
    } // 자동 회복 종료

    public bool TrySealNextFloor(string room) // 탐사 사건의 봉인을 다음 층에 보관
    { // 봉인 예약 시작
        if (HasNextSeal || string.IsNullOrWhiteSpace(room)) // 중복 봉인과 원본 방 확인
        { // 보관 불가 시작
            return false; // 먼저 보관한 봉인 유지
        } // 보관 불가 종료
        nextSealRoom = room; // 다음 층 판정의 원본 방 보관
        return true; // 다음 층 봉인 예약 완료
    } // 봉인 예약 종료

    public void AdvanceFloor() // 내려간 층에만 예약된 봉인 적용
    { // 층 변경 시작
        currentSealRoom = nextSealRoom; // 지난 층의 판정 종료와 다음 층 봉인 적용
        nextSealRoom = null; // 다음 봉인을 위한 자리 비우기
        currentSealUsed = false; // 새 층 최초 재폭풍 판정 대기
        consumedMapSeed = 0; // 지난 지도의 봉인 소모 기록 초기화
    } // 층 변경 종료

    public ExplorationEventData SelectEvent(ExplorationEventData drawn, IReadOnlyList<ExplorationEventData> pool, int mapSeed, ref bool sawEye, bool finalLayout) // 맵 생성 한 번당 첫 재폭풍의 결과만 재현
    { // 배치 판정 시작
        if (drawn == null || drawn.EventId != AshstormEyeEvent.Id || sawEye || string.IsNullOrEmpty(currentSealRoom)) // 봉인 대상과 이번 배치의 첫 재폭풍 확인
        { // 원래 배치 유지 시작
            return drawn; // 다른 사건 또는 두 번째 이후 재폭풍 유지
        } // 원래 배치 유지 종료
        sawEye = true; // 이번 맵 생성에서 첫 재폭풍 확인
        if (currentSealUsed && consumedMapSeed != mapSeed) // 같은 층에서 다른 시드로 지도를 교체한 경우
        { // 다른 지도 검사 시작
            return drawn; // 이미 사용한 봉인의 재적용 방지
        } // 다른 지도 검사 종료
        if (finalLayout) // 실제 플레이에 남는 방 역할 배치 확인
        { // 봉인 사용 기록 시작
            currentSealUsed = true; // 대체 성공과 실패 모두 봉인 한 번 소모
            consumedMapSeed = mapSeed; // 동일한 지도 복원에 사용할 시드 보관
        } // 봉인 사용 기록 종료
        var random = new System.Random(ExplorationTreasureRewardService.BuildStableHash(currentSealRoom + "|well-seal")); // 재생성과 저장 복원에서 동일한 봉인 시드
        if (random.Next(100) >= 15) // 백분율 열다섯의 대체 판정
        { // 원래 재폭풍 유지 시작
            return drawn; // 실패한 첫 판정의 재폭풍 배치
        } // 원래 재폭풍 유지 종료
        var alternatives = pool.Where(item => item != null && item.EventId != AshstormEyeEvent.Id).OrderBy(item => item.EventId, StringComparer.Ordinal).ToArray(); // 해당 층 버전에서 다른 사건만 고정 순서로 수집
        return alternatives.Length == 0 ? drawn : alternatives[random.Next(alternatives.Length)]; // 같은 봉인 원본에서 같은 대체 이벤트 선택
    } // 배치 판정 종료

    public bool IsValid(string[] party, string[] events) // 복원 전 물 보유자와 봉인 원본 확인
    { // 저장 검사 시작
        bool ownersValid = waterOwners == null || waterOwners.All(id => !string.IsNullOrWhiteSpace(id) && Array.IndexOf(party, id) >= 0) && waterOwners.Distinct().Count() == waterOwners.Count; // 출전 대상과 중복 없는 물 확인
        bool nextValid = string.IsNullOrEmpty(nextSealRoom) || Array.IndexOf(events, nextSealRoom) >= 0; // 봉인한 사건 완료 기록 확인
        bool currentValid = string.IsNullOrEmpty(currentSealRoom) ? !currentSealUsed : Array.IndexOf(events, currentSealRoom) >= 0; // 현재 봉인의 기록과 사용 상태 일치 확인
        return ownersValid && nextValid && currentValid; // 원본이 확인된 저장만 허용
    } // 저장 검사 종료
} // 우물 상태 종료
