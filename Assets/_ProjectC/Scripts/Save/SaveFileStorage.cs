using System; // 파일 검사 함수와 예외 사용
using System.IO; // 원자적 파일 교체 사용
using UnityEngine; // 저장 버전 역직렬화 사용

public static class SaveFileStorage // 저장 원본과 이전 정상본 관리
{ // 안전한 저장 파일 관리 시작
    [Serializable] // 저장 버전 확인 직렬화 지정
    private sealed class VersionProbe // 저장 파일의 형식 버전
    { // 저장 버전 정보 시작
        public int version; // 파일에 기록된 저장 버전
    } // 저장 버전 정보 종료
    public static string BackupPath(string path) // 이전 정상 저장 경로 조회
    { // 복구본 경로 조회 시작
        return string.IsNullOrWhiteSpace(path) ? null : path + ".bak"; // 유효 경로의 복구본 이름 반환
    } // 복구본 경로 조회 종료
    public static bool Exists(string path) // 원본이나 복구본 존재 확인
    { // 저장 파일 존재 확인 시작
        return !string.IsNullOrWhiteSpace(path) && (File.Exists(path) || File.Exists(BackupPath(path))); // 원본 또는 복구본 존재 여부 반환
    } // 저장 파일 존재 확인 종료
    public static bool IsUnsupportedVersion(string path) // 현재 파일이 더 새로운 저장 버전인지 확인
    { // 저장 버전 검사 시작
        try // 파일과 저장 형식 오류 처리
        { // 저장 버전 읽기 시작
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) && HasFutureVersion(File.ReadAllText(path)); // 새 저장 버전 존재 여부 반환
        } // 저장 버전 읽기 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일 읽기 오류 확인
        { // 파일 읽기 오류 처리 시작
            return false; // 새 버전 확인 실패 반환
        } // 파일 읽기 오류 처리 종료
    } // 저장 버전 검사 종료
    private static bool HasFutureVersion(string json) // 문자열의 지원 범위 밖 버전 확인
    { // 저장 버전 판별 시작
        try // 잘못된 저장 형식 처리
        { // 버전 역직렬화 시작
            VersionProbe probe = JsonUtility.FromJson<VersionProbe>(json); // 저장 버전 읽기
            return probe != null && probe.version > 1; // 현재보다 새 버전 여부 반환
        } // 버전 역직렬화 종료
        catch (ArgumentException) // 잘못된 저장 형식 확인
        { // 형식 오류 처리 시작
            return false; // 새 버전으로 확인되지 않음 반환
        } // 형식 오류 처리 종료
    } // 저장 버전 판별 종료
    public static bool IsKnownValid(string json) // 현재 지원하는 거점 또는 탐사 저장 확인
    { // 저장 내용 검증 시작
        return LobbySaveService.CanLoadJson(json) || ExplorationSaveService.CanLoadJson(json); // 실제 원본 데이터 기준 전체 검증 결과 반환
    } // 저장 내용 검증 종료
    public static bool TryRead(string path, Func<string, bool> validate, out string json, out string actualPath) // 정상 원본 또는 이전 정상본 읽기
    { // 저장 파일 읽기 시작
        json = null; // 읽기 실패 기본 내용 설정
        actualPath = null; // 읽기 실패 기본 경로 설정
        try // 파일 읽기 오류 처리
        { // 원본 검사 시작
            if (string.IsNullOrWhiteSpace(path) || validate == null) // 저장 경로와 검사 함수 확인
            { // 잘못된 입력 처리 시작
                return false; // 저장 읽기 실패 반환
            } // 잘못된 입력 처리 종료
            if (File.Exists(path)) // 현재 저장 원본 존재 확인
            { // 현재 저장 검사 시작
                string primary = File.ReadAllText(path); // 현재 저장 내용 읽기
                if (HasFutureVersion(primary)) // 지원하지 않는 새 버전 확인
                { // 새 버전 보존 시작
                    return false; // 이전 복구본 자동 사용 차단
                } // 새 버전 보존 종료
                if (validate(primary)) // 현재 저장 내용 검증
                { // 정상 원본 반환 시작
                    json = primary; // 정상 원본 내용 보관
                    actualPath = path; // 정상 원본 경로 보관
                    return true; // 정상 원본 읽기 성공 반환
                } // 정상 원본 반환 종료
                if (IsKnownValid(primary)) // 다른 종류의 정상 저장 원본 확인
                { // 다른 정상 저장 보호 시작
                    return false; // 다른 종류 복구본의 잘못된 우선 사용 차단
                } // 다른 정상 저장 보호 종료
            } // 현재 저장 검사 종료
            string backup = BackupPath(path); // 이전 정상본 경로 조회
            if (File.Exists(backup)) // 이전 정상본 존재 확인
            { // 복구본 검사 시작
                string candidate = File.ReadAllText(backup); // 이전 정상본 내용 읽기
                if (!HasFutureVersion(candidate) && validate(candidate)) // 지원 버전과 전체 유효성 확인
                { // 정상 복구본 반환 시작
                    json = candidate; // 정상 복구본 내용 보관
                    actualPath = backup; // 정상 복구본 경로 보관
                    return true; // 정상 복구본 읽기 성공 반환
                } // 정상 복구본 반환 종료
            } // 복구본 검사 종료
            return false; // 정상 저장 파일 없음 반환
        } // 원본 검사 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일과 경로 오류 확인
        { // 파일 읽기 오류 처리 시작
            return false; // 저장 읽기 실패 반환
        } // 파일 읽기 오류 처리 종료
    } // 저장 파일 읽기 종료
    public static bool Write(string path, string json) // 임시 파일을 이용한 안전한 저장
    { // 안전 저장 시작
        string temporaryPath = null; // 임시 파일 경로 준비
        try // 저장 파일 오류 처리
        { // 저장 내용 준비 시작
            if (string.IsNullOrWhiteSpace(path) || !IsKnownValid(json)) // 저장 경로와 새 내용 유효성 확인
            { // 잘못된 저장 처리 시작
                return false; // 기존 파일 유지
            } // 잘못된 저장 처리 종료
            string fullPath = Path.GetFullPath(path); // 절대 저장 경로 계산
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)); // 저장 폴더 준비
            temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp"; // 고유 임시 저장 경로 생성
            File.WriteAllText(temporaryPath, json); // 임시 파일에 정상 저장 내용 기록
            if (File.Exists(fullPath)) // 현재 저장 원본 존재 확인
            { // 기존 파일 교체 시작
                string old = File.ReadAllText(fullPath); // 현재 저장 내용 조회
                if (HasFutureVersion(old)) // 교체 직전 읽은 원본에 지원하지 않는 버전 존재 확인
                { // 새 버전 덮어쓰기 방지 시작
                    return false; // 원본과 복구본 유지 및 임시본 정리
                } // 새 버전 덮어쓰기 방지 종료
                string backup = IsKnownValid(old) ? BackupPath(fullPath) : null; // 정상 원본만 이전 저장으로 보관
                if (backup != null && IsUnsupportedVersion(backup)) // 복구본의 새 저장 버전 확인
                { // 새 버전 복구본 보호 시작
                    return false; // 지원하지 않는 복구본 덮어쓰기 차단
                } // 새 버전 복구본 보호 종료
                File.Replace(temporaryPath, fullPath, backup); // 원본과 이전 정상본을 원자적으로 교체
            } // 기존 파일 교체 종료
            else // 현재 저장 원본 없음 확인
            { // 첫 저장 시작
                File.Move(temporaryPath, fullPath); // 임시 파일을 원본 저장으로 이동
            } // 첫 저장 종료
            temporaryPath = null; // 임시 파일 이동 완료 표시
            return true; // 저장 성공 반환
        } // 저장 내용 준비 종료
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) // 파일과 경로 오류 확인
        { // 저장 오류 처리 시작
            Debug.LogWarning("[SaveFile] 저장 실패: " + exception.Message); // 저장 오류 안내
            return false; // 기존 저장 유지
        } // 저장 오류 처리 종료
        finally // 남은 임시 파일 정리
        { // 임시 파일 정리 시작
            if (temporaryPath != null && File.Exists(temporaryPath)) // 임시 파일 존재 확인
            { // 남은 임시 파일 제거 시작
                try // 임시 파일 제거 오류 처리
                { // 임시 파일 제거 시작
                    File.Delete(temporaryPath); // 불완전 임시 파일 삭제
                } // 임시 파일 제거 종료
                catch (IOException) // 입출력 오류 확인
                { // 입출력 오류 무시 시작
                } // 입출력 오류 무시 종료
                catch (UnauthorizedAccessException) // 파일 권한 오류 확인
                { // 파일 권한 오류 무시 시작
                } // 파일 권한 오류 무시 종료
            } // 남은 임시 파일 제거 종료
        } // 임시 파일 정리 종료
    } // 안전 저장 종료
} // 안전한 저장 파일 관리 종료
