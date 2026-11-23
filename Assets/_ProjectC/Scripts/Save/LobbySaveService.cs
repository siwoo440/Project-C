using System; // 기본 자료형과 예외 사용
using System.Collections.Generic; // 목록과 중복 검사 사용
using System.IO; // 저장 파일 입출력 사용
using UnityEngine; // 유니티 리소스와 직렬화 사용

public static class LobbySaveService // 거점 상태 저장 서비스
{ // 저장 서비스 시작
    public static string DefaultPath => Path.Combine(Application.persistentDataPath, "lobby-save.json"); // 기본 거점 저장 경로
    [Serializable] // 저장 직렬화 지정
    private sealed class SaveData // 거점 전체 저장 정보
    { // 저장 정보 시작
        public int version; // 저장 형식 버전
        public int gold; // 저장 골드
        public int screw; // 저장 나사
        public int ironPlate; // 저장 철판
        public int wire; // 저장 전선
        public int level; // 저장 성장 레벨
        public int experience; // 저장 경험치
        public int affinity; // 저장 호감도
        public FacilityState[] facilities; // 저장 설비 목록
        public CharacterState[] characters; // 저장 캐릭터 목록
    } // 저장 정보 종료
    [Serializable] // 설비 직렬화 지정
    private sealed class FacilityState // 설비 저장 정보
    { // 설비 정보 시작
        public int type; // 설비 종류 번호
        public int level; // 설비 강화 단계
    } // 설비 정보 종료
    [Serializable] // 캐릭터 직렬화 지정
    private sealed class CharacterState // 캐릭터 저장 정보
    { // 캐릭터 정보 시작
        public string id; // 캐릭터 식별자
        public int health; // 저장 체력
        public int mental; // 저장 정신력
        public int deaths; // 저장 사망 횟수
        public int recovery; // 남은 회복 탐사 횟수
    } // 캐릭터 정보 종료
    public static bool Save(string path) // 현재 거점 상태 저장
    { // 저장 시작
        string temporaryPath = null; // 임시 저장 경로 준비
        try // 파일 오류 처리 시작
        { // 저장 처리 시작
            CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 전체 캐릭터 목록 조회
            if (roster == null || string.IsNullOrWhiteSpace(path)) // 필수 정보 확인
            { // 필수 정보 누락 시작
                return false; // 저장 실패 반환
            } // 필수 정보 누락 종료
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            FacilityUpgradeManager facilities = FacilityUpgradeManager.EnsureInstance(); // 설비 관리자 준비
            CharacterProgressionManager progression = CharacterProgressionManager.EnsureInstance(); // 성장 관리자 준비
            CharacterAffinityManager affinity = CharacterAffinityManager.EnsureInstance(); // 호감도 관리자 준비
            BattleResultManager result = BattleResultManager.EnsureInstance(); // 캐릭터 상태 관리자 준비
            CharacterRecoveryManager recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
            List<FacilityState> facilityStates = new List<FacilityState>(); // 설비 저장 목록 생성
            foreach (FacilityType type in Enum.GetValues(typeof(FacilityType))) // 전체 설비 종류 순회
            { // 설비 수집 시작
                facilityStates.Add(new FacilityState { type = (int)type, level = facilities.GetLevel(type) }); // 설비 상태 기록
            } // 설비 수집 종료
            List<CharacterState> characterStates = new List<CharacterState>(); // 캐릭터 저장 목록 생성
            foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
            { // 캐릭터 수집 시작
                if (character == null || !result.TryGetSavedAllyHealth(character.CharacterId, out int health)) // 저장 상태 존재 확인
                { // 저장 상태 없음 시작
                    continue; // 다음 캐릭터 확인
                } // 저장 상태 없음 종료
                if (!result.TryGetSavedAllyMental(character.CharacterId, out int mental)) // 저장 정신력 존재 확인
                { // 정신력 누락 시작
                    return false; // 불완전 상태 저장 차단
                } // 정신력 누락 종료
                characterStates.Add(new CharacterState { id = character.CharacterId, health = health, mental = mental, deaths = result.GetDeathCount(character.CharacterId), recovery = recovery.GetRemainingRecoveryExpeditions(character) }); // 캐릭터 상태 기록
            } // 캐릭터 수집 종료
            SaveData data = new SaveData { version = 1, gold = resources.Gold, screw = resources.Screw, ironPlate = resources.IronPlate, wire = resources.Wire, level = progression.Level, experience = progression.CurrentExperience, affinity = affinity.Affinity, facilities = facilityStates.ToArray(), characters = characterStates.ToArray() }; // 전체 거점 상태 구성
            if (!IsValid(data, roster)) // 저장 상태의 유효성 확인
            { // 잘못된 상태 처리 시작
                return false; // 기존 저장 유지
            } // 잘못된 상태 처리 종료
            string fullPath = Path.GetFullPath(path); // 절대 저장 경로 계산
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)); // 저장 폴더 준비
            temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp"; // 고유 임시 파일 경로 생성
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data)); // 임시 파일에 저장 내용 기록
            if (File.Exists(fullPath)) // 기존 저장 파일 존재 확인
            { // 기존 저장 교체 시작
                File.Replace(temporaryPath, fullPath, null); // 기존 파일 원자적 교체
            } // 기존 저장 교체 종료
            else // 기존 저장 파일 없음 확인
            { // 첫 저장 시작
                File.Move(temporaryPath, fullPath); // 임시 파일을 저장 파일로 이동
            } // 첫 저장 종료
            temporaryPath = null; // 임시 파일 이동 완료 표시
            return true; // 저장 성공 반환
        } // 저장 처리 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 경로와 입출력 오류 확인
        { // 저장 오류 시작
            Debug.LogWarning("[LobbySave] 저장 실패: " + exception.Message); // 저장 오류 안내
            return false; // 저장 실패 반환
        } // 저장 오류 종료
        finally // 임시 파일 정리 보장
        { // 임시 파일 정리 시작
            if (temporaryPath != null && File.Exists(temporaryPath)) // 남은 임시 파일 확인
            { // 남은 파일 정리 시작
                try // 임시 파일 제거 시도
                { // 임시 파일 제거 시작
                    File.Delete(temporaryPath); // 남은 임시 파일 제거
                } // 임시 파일 제거 종료
                catch (IOException) // 파일 오류 확인
                { // 파일 오류 처리 시작
                } // 파일 오류 처리 종료
                catch (UnauthorizedAccessException) // 권한 오류 확인
                { // 권한 오류 처리 시작
                } // 권한 오류 처리 종료
            } // 남은 파일 정리 종료
        } // 임시 파일 정리 종료
    } // 저장 종료
    public static bool Load(string path) // 저장된 거점 상태 복원
    { // 복원 시작
        try // 파일 오류 처리 시작
        { // 복원 처리 시작
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) // 저장 파일 존재 확인
            { // 파일 없음 시작
                return false; // 복원 실패 반환
            } // 파일 없음 종료
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path)); // 저장 파일 읽기
            CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 전체 캐릭터 목록 조회
            if (!IsValid(data, roster)) // 저장 데이터 전체 검증
            { // 유효하지 않은 저장 시작
                return false; // 기존 상태 유지
            } // 유효하지 않은 저장 종료
            PlayerResourceManager.EnsureInstance().RestoreResources(data.gold, data.screw, data.ironPlate, data.wire); // 자원 복원
            FacilityUpgradeManager facilities = FacilityUpgradeManager.EnsureInstance(); // 설비 관리자 준비
            foreach (FacilityState facility in data.facilities) // 저장 설비 순회
            { // 설비 복원 시작
                facilities.RestoreFacilityLevel((FacilityType)facility.type, facility.level); // 저장 단계 적용
            } // 설비 복원 종료
            CharacterProgressionManager.EnsureInstance().RestoreProgress(data.level, data.experience); // 성장 복원
            CharacterAffinityManager.EnsureInstance().RestoreAffinity(data.affinity); // 호감도 복원
            BattleResultManager result = BattleResultManager.EnsureInstance(); // 캐릭터 상태 관리자 준비
            CharacterRecoveryManager recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
            result.DiscardPendingResult(); // 이전 전투 결과 제거
            result.ResetSavedPartyState(); // 기존 캐릭터 상태 제거
            recovery.ResetRecoveryState(); // 기존 회복 상태 제거
            foreach (CharacterState state in data.characters) // 저장 캐릭터 순회
            { // 캐릭터 복원 시작
                CharacterData character = FindCharacter(roster, state.id); // 저장 캐릭터 원본 조회
                result.RestoreSavedAllyState(character, state.health, state.mental, state.deaths); // 캐릭터 상태 복원
                if (state.recovery > 0) // 회복 중 캐릭터 확인
                { // 회복 복원 시작
                    recovery.RestoreRecoveryState(character, state.recovery); // 남은 회복 횟수 복원
                } // 회복 복원 종료
            } // 캐릭터 복원 종료
            return true; // 복원 성공 반환
        } // 복원 처리 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 또는 저장 형식 오류 확인
        { // 복원 오류 시작
            Debug.LogWarning("[LobbySave] 복원 실패: " + exception.Message); // 복원 오류 안내
            return false; // 복원 실패 반환
        } // 복원 오류 종료
    } // 복원 종료
    private static bool IsValid(SaveData data, CharacterRosterData roster) // 저장 정보 유효성 검사
    { // 유효성 검사 시작
        if (data == null || roster == null || data.version != 1 || data.facilities == null || data.characters == null) // 필수 데이터와 버전 확인
        { // 필수 정보 오류 시작
            return false; // 유효하지 않은 저장 반환
        } // 필수 정보 오류 종료
        if (data.gold < 0 || data.screw < 0 || data.ironPlate < 0 || data.wire < 0 || data.level < 1 || data.level > 100000 || data.experience < 0 || data.experience >= 20L + (data.level - 1L) * 10L || data.affinity < 0) // 숫자 범위 확인
        { // 숫자 오류 시작
            return false; // 유효하지 않은 저장 반환
        } // 숫자 오류 종료
        HashSet<int> facilityTypes = new HashSet<int>(); // 설비 중복 검사 목록
        foreach (FacilityState facility in data.facilities) // 저장 설비 순회
        { // 설비 검사 시작
            if (facility == null || !Enum.IsDefined(typeof(FacilityType), facility.type) || !facilityTypes.Add(facility.type) || facility.level < 0 || facility.level > FacilityUpgradeManager.MaximumLevel) // 설비 종류와 단계 검사
            { // 설비 오류 시작
                return false; // 유효하지 않은 저장 반환
            } // 설비 오류 종료
        } // 설비 검사 종료
        if (facilityTypes.Count != Enum.GetValues(typeof(FacilityType)).Length) // 설비 누락 확인
        { // 설비 누락 시작
            return false; // 유효하지 않은 저장 반환
        } // 설비 누락 종료
        HashSet<string> characterIds = new HashSet<string>(); // 캐릭터 중복 검사 목록
        foreach (CharacterState state in data.characters) // 저장 캐릭터 순회
        { // 캐릭터 검사 시작
            CharacterData character = state == null ? null : FindCharacter(roster, state.id); // 캐릭터 원본 조회
            if (character == null || !characterIds.Add(state.id) || state.health < 0 || state.mental < BattleMentalRuntime.MinimumMental || state.mental > BattleMentalRuntime.MaximumMental || state.deaths < 0 || state.recovery < 0 || state.recovery > CharacterRecoveryManager.PrototypeRecoveryExpeditionCount || (state.recovery > 0 && state.health > 0)) // 캐릭터 상태 범위 검사
            { // 캐릭터 오류 시작
                return false; // 유효하지 않은 저장 반환
            } // 캐릭터 오류 종료
        } // 캐릭터 검사 종료
        return true; // 전체 검사 성공 반환
    } // 유효성 검사 종료
    private static CharacterData FindCharacter(CharacterRosterData roster, string id) // 식별자로 캐릭터 조회
    { // 캐릭터 조회 시작
        foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
        { // 캐릭터 비교 시작
            if (character != null && character.CharacterId == id) // 식별자 일치 확인
            { // 일치 처리 시작
                return character; // 일치 캐릭터 반환
            } // 일치 처리 종료
        } // 캐릭터 비교 종료
        return null; // 일치 캐릭터 없음 반환
    } // 캐릭터 조회 종료
} // 저장 서비스 종료
