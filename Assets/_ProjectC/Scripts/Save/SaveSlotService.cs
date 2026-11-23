using System; // 파일 오류와 시간 사용
using System.IO; // 슬롯 파일 경로 사용
using UnityEngine; // 탐사 위치와 저장 경로 사용

public sealed class SaveSlotService // 자동 저장과 수동 슬롯 관리
{ // 슬롯 서비스 시작
    private readonly string directory; // 수동 저장 폴더
    private readonly string autoLobbyPath; // 자동 거점 저장 경로
    private readonly string autoExplorationPath; // 자동 탐사 저장 경로
    public SaveSlotService(string directory, string autoLobbyPath, string autoExplorationPath) // 슬롯 경로 설정
    { // 슬롯 경로 설정 시작
        this.directory = directory; // 수동 저장 폴더 보관
        this.autoLobbyPath = autoLobbyPath; // 자동 거점 저장 경로 보관
        this.autoExplorationPath = autoExplorationPath; // 자동 탐사 저장 경로 보관
    } // 슬롯 경로 설정 종료
    public static SaveSlotService CreateDefault() // 기본 저장 경로 서비스 생성
    { // 기본 서비스 생성 시작
        return new SaveSlotService(Path.Combine(Application.persistentDataPath, "save-slots"), LobbySaveService.DefaultPath, ExplorationSaveService.DefaultPath); // 실제 저장 경로 연결
    } // 기본 서비스 생성 종료
    public bool SaveLobby(int index) // 거점 상태를 슬롯에 저장
    { // 거점 슬롯 저장 시작
        string path = GetWritePath(index, false); // 거점 저장 경로 조회
        return path != null && LobbySaveService.Save(path); // 거점 저장 결과 반환
    } // 거점 슬롯 저장 종료
    public bool SaveExploration(int index, Vector3 position) // 탐사 상태를 슬롯에 저장
    { // 탐사 슬롯 저장 시작
        string path = GetWritePath(index, true); // 탐사 저장 경로 조회
        return path != null && ExplorationSaveService.Save(path, position); // 탐사 저장 결과 반환
    } // 탐사 슬롯 저장 종료
    public bool CanLoad(int index) // 슬롯 복원 가능 여부 확인
    { // 슬롯 복원 검사 시작
        return GetReadablePath(index) != null; // 유효한 파일 존재 반환
    } // 슬롯 복원 검사 종료
    public bool IsExploration(int index) // 슬롯의 탐사 저장 여부 확인
    { // 탐사 슬롯 검사 시작
        string path = GetReadablePath(index); // 유효한 저장 경로 조회
        return path != null && ExplorationSaveService.CanLoad(path); // 탐사 파일 유효성 반환
    } // 탐사 슬롯 검사 종료
    public bool HasFile(int index) // 슬롯 파일 존재 여부 확인
    { // 슬롯 파일 확인 시작
        if (!IsValidIndex(index)) // 슬롯 번호 범위 확인
        { // 잘못된 번호 처리 시작
            return false; // 파일 없음 반환
        } // 잘못된 번호 처리 종료
        return index == 0 ? SaveFileStorage.Exists(autoLobbyPath) || SaveFileStorage.Exists(autoExplorationPath) : SaveFileStorage.Exists(ManualPath(index)); // 원본과 복구본을 포함한 존재 여부 반환
    } // 슬롯 파일 확인 종료
    public bool IsRecovery(int index) // 이전 정상 복구본 사용 여부 확인
    { // 복구본 사용 확인 시작
        string path = GetReadablePath(index); // 현재 슬롯의 저장 경로 조회
        return path != null && SaveFileStorage.TryRead(path, SaveFileStorage.IsKnownValid, out string ignored, out string actualPath) && actualPath == SaveFileStorage.BackupPath(path); // 복구본 경로 사용 여부 반환
    } // 복구본 사용 확인 종료
    public bool IsUnsupportedVersion(int index) // 지원하지 않는 새 버전 저장 여부 확인
    { // 저장 버전 확인 시작
        if (!IsValidIndex(index) || GetReadablePath(index) != null) // 유효한 슬롯과 복원 가능 파일 확인
        { // 새 버전 표시 불필요 처리 시작
            return false; // 지원하지 않는 버전 표시 생략
        } // 새 버전 표시 불필요 처리 종료
        return index == 0 ? SaveFileStorage.IsUnsupportedVersion(autoLobbyPath) || SaveFileStorage.IsUnsupportedVersion(autoExplorationPath) : SaveFileStorage.IsUnsupportedVersion(ManualPath(index)); // 새 버전 원본 존재 여부 반환
    } // 저장 버전 확인 종료
    public bool Load(int index) // 슬롯 저장 상태 복원
    { // 슬롯 복원 시작
        string path = GetReadablePath(index); // 유효한 저장 경로 조회
        if (path == null) // 유효한 저장 파일 확인
        { // 복원 불가 처리 시작
            return false; // 기존 상태 유지
        } // 복원 불가 처리 종료
        return ExplorationSaveService.CanLoad(path) ? ExplorationSaveService.Load(path) : LobbySaveService.Load(path); // 저장 종류에 맞는 복원 실행
    } // 슬롯 복원 종료
    public bool Delete(int index) // 슬롯 저장 파일 삭제
    { // 슬롯 삭제 시작
        if (!IsValidIndex(index)) // 슬롯 번호 범위 확인
        { // 잘못된 번호 처리 시작
            return false; // 삭제 실패 반환
        } // 잘못된 번호 처리 종료
        try // 삭제 오류 처리 시작
        { // 슬롯 파일 삭제 시작
            if (index == 0) // 자동 저장 슬롯 확인
            { // 자동 저장 삭제 시작
                DeleteFile(autoLobbyPath); // 자동 거점 저장 삭제
                DeleteFile(autoExplorationPath); // 자동 탐사 저장 삭제
            } // 자동 저장 삭제 종료
            else // 수동 저장 슬롯 확인
            { // 수동 저장 삭제 시작
                DeleteFile(ManualPath(index)); // 선택한 수동 저장 삭제
            } // 수동 저장 삭제 종료
            return true; // 삭제 성공 반환
        } // 슬롯 파일 삭제 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 오류 확인
        { // 삭제 오류 처리 시작
            Debug.LogWarning("[SaveSlot] 삭제 실패: " + exception.Message); // 삭제 실패 안내
            return false; // 삭제 실패 반환
        } // 삭제 오류 처리 종료
    } // 슬롯 삭제 종료
    public string GetTimestamp(int index) // 슬롯 저장 시각 표시
    { // 저장 시각 조회 시작
        string path = GetReadablePath(index); // 유효한 저장 경로 조회
        if (path == null) // 유효한 저장 파일 확인
        { // 저장 없음 처리 시작
            return string.Empty; // 빈 시각 반환
        } // 저장 없음 처리 종료
        try // 파일 시각 오류 처리 시작
        { // 저장 시각 읽기 시작
            return SaveFileStorage.TryRead(path, SaveFileStorage.IsKnownValid, out string ignored, out string actualPath) ? File.GetLastWriteTime(actualPath).ToString("yyyy/MM/dd HH:mm") : string.Empty; // 실제 원본 또는 복구본의 저장 시각 반환
        } // 저장 시각 읽기 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 시각 오류 확인
        { // 시각 오류 처리 시작
            return string.Empty; // 빈 시각 반환
        } // 시각 오류 처리 종료
    } // 저장 시각 조회 종료
    private string GetReadablePath(int index) // 슬롯의 유효한 저장 경로 조회
    { // 유효 경로 조회 시작
        if (!IsValidIndex(index)) // 슬롯 번호 범위 확인
        { // 잘못된 번호 처리 시작
            return null; // 유효 경로 없음 반환
        } // 잘못된 번호 처리 종료
        if (index != 0) // 수동 저장 슬롯 확인
        { // 수동 경로 검사 시작
            string path = ManualPath(index); // 수동 파일 경로 조회
            return IsValidSave(path) ? path : null; // 유효한 수동 파일 반환
        } // 수동 경로 검사 종료
        if (SaveFileStorage.IsUnsupportedVersion(autoLobbyPath) || SaveFileStorage.IsUnsupportedVersion(autoExplorationPath)) // 자동 저장 중 새 버전 원본 존재 확인
        { // 새 버전 자동 저장 보호 시작
            return null; // 다른 이전 자동 저장으로의 임의 복원 차단
        } // 새 버전 자동 저장 보호 종료
        bool lobbyValid = LobbySaveService.CanLoad(autoLobbyPath); // 자동 거점 저장 검사
        bool explorationValid = ExplorationSaveService.CanLoad(autoExplorationPath); // 자동 탐사 저장 검사
        if (!lobbyValid || !explorationValid) // 한쪽 저장만 유효한지 확인
        { // 단일 유효 저장 처리 시작
            return explorationValid ? autoExplorationPath : lobbyValid ? autoLobbyPath : null; // 유효한 저장 경로 반환
        } // 단일 유효 저장 처리 종료
        try // 저장 시각 오류 처리 시작
        { // 자동 저장 시각 비교 시작
            SaveFileStorage.TryRead(autoExplorationPath, ExplorationSaveService.CanLoadJson, out string runJson, out string runPath); // 실제 탐사 원본 또는 복구본 경로 확인
            SaveFileStorage.TryRead(autoLobbyPath, LobbySaveService.CanLoadJson, out string lobbyJson, out string lobbyPath); // 실제 거점 원본 또는 복구본 경로 확인
            return File.GetLastWriteTimeUtc(runPath) >= File.GetLastWriteTimeUtc(lobbyPath) ? autoExplorationPath : autoLobbyPath; // 실제 파일 시각 기준 최근 자동 저장 반환
        } // 자동 저장 시각 비교 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 시각 오류 확인
        { // 시각 오류 처리 시작
            return autoLobbyPath; // 유효한 거점 저장 반환
        } // 시각 오류 처리 종료
    } // 유효 경로 조회 종료
    private string GetWritePath(int index, bool exploration) // 저장 대상 경로 조회
    { // 저장 경로 조회 시작
        if (!IsValidIndex(index)) // 슬롯 번호 범위 확인
        { // 잘못된 번호 처리 시작
            return null; // 저장 경로 없음 반환
        } // 잘못된 번호 처리 종료
        return index == 0 ? exploration ? autoExplorationPath : autoLobbyPath : ManualPath(index); // 자동 또는 수동 경로 반환
    } // 저장 경로 조회 종료
    private string ManualPath(int index) // 수동 슬롯 파일 경로 생성
    { // 수동 경로 생성 시작
        return Path.Combine(directory, "slot-" + index + ".json"); // 슬롯 번호별 경로 반환
    } // 수동 경로 생성 종료
    private static bool IsValidIndex(int index) // 슬롯 번호 범위 검사
    { // 슬롯 번호 검사 시작
        return index >= 0 && index < SaveMenuView.SlotCount; // 자동 저장과 네 슬롯 범위 반환
    } // 슬롯 번호 검사 종료
    private static bool IsValidSave(string path) // 저장 파일 전체 유효성 검사
    { // 저장 파일 검사 시작
        return LobbySaveService.CanLoad(path) || ExplorationSaveService.CanLoad(path); // 거점 또는 탐사 저장 여부 반환
    } // 저장 파일 검사 종료
    private static void DeleteFile(string path) // 지정 파일 삭제
    { // 파일 삭제 시작
        string backup = SaveFileStorage.BackupPath(path); // 지정 저장의 복구본 경로 조회
        if (!string.IsNullOrWhiteSpace(backup) && File.Exists(backup)) // 복구본 존재 여부 확인
        { // 복구본 삭제 시작
            File.Delete(backup); // 이전 정상 복구본 삭제
        } // 복구본 삭제 종료
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) // 실제 파일 존재 확인
        { // 파일 제거 시작
            File.Delete(path); // 지정 저장 파일 제거
        } // 파일 제거 종료
    } // 파일 삭제 종료
} // 슬롯 서비스 종료
