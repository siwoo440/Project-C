using System.Collections.Generic; // 지역별 조우 원본 목록
using UnityEngine; // Resources의 실제 Unity 조우 에셋 사용

public static class ExpeditionZoneEncounters // 출정 지역의 실제 조우 후보와 필수 보스 결정
{ // 지역 조우 조회 시작
    private const string WinterfallBossId = "ENC_WF_ASHSTORM_CORE"; // 새 출정과 기존 저장이 공유하는 윈터폴 필수 보스
    private const string RegionalBossSuffix = "_BOSS"; // 지역 조우의 하나뿐인 고정 보스 원본 접미어

    public static List<EncounterData> Load(ExpeditionZoneId zone, int poolVersion) // 현재 지역과 저장 버전에서 사용할 조우 불러오기
    { // 지역별 유효 조우 로드 시작
        string path = zone == ExpeditionZoneId.WinterfallAshPlains // 기존 윈터폴의 저장 버전 호환 여부
            ? "Encounters" : "Exploration/Regions/" + ExpeditionZones.Get(zone).VisualKey + "/Encounters"; // 윈터폴 원본 또는 독립 지역의 실제 리소스 경로
        var source = Resources.LoadAll<EncounterData>(path); // 지정 위치의 Unity 조우 원본 조회
        var result = new List<EncounterData>(source.Length); // 버전별 유효 조우 후보
        foreach (var encounter in source) // 지정한 지역의 원본만 순회
        { // 유효 원본 확인 시작
            if (encounter != null && encounter.IsAvailableForPoolVersion(poolVersion) && encounter.IsValidData()) // 저장 버전 및 실제 적 참조가 있는 후보 확인
            { // 현재 선택 가능한 조우 등록 시작
                result.Add(encounter); // 현재 지역의 유효 조우 후보
            } // 현재 선택 가능한 조우 등록 종료
        } // 유효 원본 확인 종료
        result.Sort((left, right) => string.CompareOrdinal(left.EncounterId, right.EncounterId)); // 같은 시드의 원본 선택 순서 유지
        return result; // 지역과 저장 버전이 확정된 후보 반환
    } // 지역별 유효 조우 로드 종료

    public static EncounterData GetMandatoryBoss(ExpeditionZoneId zone, int poolVersion) // 현재 지역의 의도된 필수 보스 원본 조회
    { // 고정 보스의 원형 탐색 시작
        string bossId = GetMandatoryBossId(zone); // 지역별 고정 보스의 실제 ID
        foreach (var encounter in Load(zone, poolVersion)) // 현재 지역만 대상으로 한 실제 조우 목록
        { // 필수 보스 확인 시작
            if (encounter.BattleType == BattleType.Boss && encounter.EncounterId == bossId) // 등급과 고유 원본이 모두 일치하는지 확인
            { // 현재 지역 필수 보스 확인 시작
                return encounter; // 고정 필수 보스 원본 반환
            } // 현재 지역 필수 보스 확인 종료
        } // 필수 보스 확인 종료
        return null; // 현재 원본이 없는 지역 또는 호환 저장의 대기 상태
    } // 고정 보스의 원형 탐색 종료

    public static bool IsMandatoryBoss(ExpeditionZoneId zone, EncounterData encounter) // 현재 지역의 지정 보스 전투인지 확인
    { // 실제 승리 조우의 지역 확인 시작
        return encounter != null && encounter.BattleType == BattleType.Boss && encounter.EncounterId == GetMandatoryBossId(zone); // 선택 또는 다른 지역 조우를 제외한 원본 ID 검사
    } // 실제 승리 조우의 지역 확인 종료

    public static List<EncounterData> LoadRandom(ExpeditionZoneId zone, int poolVersion) // 일반 방에서 고를 현재 지역 데이터
    { // 일반 방의 무작위 목록 시작
        var candidates = Load(zone, poolVersion); // 저장 시점과 같은 지역의 모든 원본
        if (zone != ExpeditionZoneId.WinterfallAshPlains) // 기존 윈터폴 저장이 아닌 지역의 분리 대상 확인
        { // 예약 보스와 최종 정예를 일반 목록에서 제외 시작
            candidates.RemoveAll(encounter => encounter.BattleType == BattleType.Boss || IsRegionalEliteGate(zone, encounter)); // 별도 조우가 일반 방이나 미예약 방에 나오지 않도록 제한
        } // 예약 보스와 최종 정예를 일반 목록에서 제외 종료
        return candidates; // 지역별 일반 조우 역할에 쓸 후보
    } // 일반 방의 무작위 목록 종료

    public static EncounterData GetRegionalEliteGate(ExpeditionZoneId zone, int poolVersion) // 새 지역 탐사를 종료할 마지막 정예 관문의 원본 조회
    { // 별도로 지정한 최종 정예 탐색 시작
        if (zone == ExpeditionZoneId.WinterfallAshPlains) // 원래 윈터폴의 필수 보스 관문과 분리
        { // 기존 지역의 정예 대체 없음 처리 시작
            return null; // 원래 보스 승리로 탐사를 완료하는 지역
        } // 기존 지역의 정예 대체 없음 처리 종료
        foreach (var encounter in Load(zone, poolVersion)) // 자신의 지역과 버전의 후보만 조회
        { // 최종 정예 원본 확인 시작
            if (IsRegionalEliteGate(zone, encounter)) // 이름과 전투 등급을 함께 확인
            { // 실제 최종 정예 확인 시작
                return encounter; // 따로 구분한 마지막 관문 원본
            } // 실제 최종 정예 확인 종료
        } // 최종 정예 원본 확인 종료
        return null; // 원본이 없는 호환 저장 데이터
    } // 별도로 지정한 최종 정예 탐색 종료

    public static bool IsRegionalEliteGate(ExpeditionZoneId zone, EncounterData encounter) // 지역 미예약 출정을 끝낼 실제 고정 정예 조우 확인
    { // 지정한 최종 관문 조우 ID 확인 시작
        return zone != ExpeditionZoneId.WinterfallAshPlains && encounter != null && encounter.BattleType == BattleType.Elite && // 현재 지역의 정예 원본만 사용
            encounter.EncounterId == "ENC_ZONE_" + ExpeditionZones.Get(zone).VisualKey + "_ELITE_GATE"; // 보통 정예와 마지막 정예의 독립 원본 ID
    } // 지정한 최종 관문 조우 ID 확인 종료


    private static string GetMandatoryBossId(ExpeditionZoneId zone) // 특정 지역에 배정한 필수 보스 조우 ID
    { // 기존 저장 ID와 새 지역 ID 구분 시작
        return zone == ExpeditionZoneId.WinterfallAshPlains ? WinterfallBossId : "ENC_ZONE_" + ExpeditionZones.Get(zone).VisualKey + RegionalBossSuffix; // 오래된 윈터폴 원본과 신규 지역 고정 규칙
    } // 기존 저장 ID와 새 지역 ID 구분 종료
} // 지역 조우 조회 종료
