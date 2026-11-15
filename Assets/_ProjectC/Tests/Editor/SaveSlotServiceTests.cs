using System; // 기본 자료형 사용
using System.Collections; // 코루틴 사용
using System.IO; // 임시 저장 파일 사용
using NUnit.Framework; // 테스트 검증 사용
using UnityEngine; // 유니티 경로 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용

public sealed class SaveSlotServiceTests // 저장 슬롯 서비스 검증
{ // 저장 슬롯 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator ManualSlots_SaveRestoreAndDeleteIndependently() // 수동 슬롯 독립 동작 확인
    { // 수동 슬롯 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string directory = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 독립 임시 폴더 생성
        Directory.CreateDirectory(directory); // 테스트 저장 폴더 준비
        try // 테스트 폴더 정리 보장
        { // 슬롯 검사 시작
            Type serviceType = typeof(MainMenuController).Assembly.GetType("SaveSlotService"); // 슬롯 서비스 형식 조회
            Assert.IsNotNull(serviceType); // 슬롯 서비스 존재 확인
            object service = Activator.CreateInstance(serviceType, directory, Path.Combine(directory, "auto-lobby.json"), Path.Combine(directory, "auto-run.json")); // 독립 저장 서비스 생성
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.ResetResources(); // 자원 초기화
            resources.AddResources(31, 0, 0, 0); // 첫 슬롯 골드 설정
            int firstGold = resources.Gold; // 첫 슬롯 골드 보관
            Assert.IsTrue((bool)serviceType.GetMethod("SaveLobby").Invoke(service, new object[] { 1 })); // 첫 슬롯 저장 확인
            resources.AddResources(17, 0, 0, 0); // 둘째 슬롯 골드 설정
            int secondGold = resources.Gold; // 둘째 슬롯 골드 보관
            Assert.IsTrue((bool)serviceType.GetMethod("SaveLobby").Invoke(service, new object[] { 2 })); // 둘째 슬롯 저장 확인
            resources.ResetResources(); // 현재 자원 변경
            Assert.IsTrue((bool)serviceType.GetMethod("Load").Invoke(service, new object[] { 1 })); // 첫 슬롯 불러오기 확인
            Assert.AreEqual(firstGold, resources.Gold); // 첫 슬롯 자원 복원 확인
            Assert.IsTrue((bool)serviceType.GetMethod("Delete").Invoke(service, new object[] { 1 })); // 첫 슬롯 삭제 확인
            Assert.IsFalse((bool)serviceType.GetMethod("CanLoad").Invoke(service, new object[] { 1 })); // 첫 슬롯 삭제 상태 확인
            Assert.IsTrue((bool)serviceType.GetMethod("Load").Invoke(service, new object[] { 2 })); // 둘째 슬롯 유지 확인
            Assert.AreEqual(secondGold, resources.Gold); // 둘째 슬롯 자원 복원 확인
            Assert.IsFalse((bool)serviceType.GetMethod("SaveLobby").Invoke(service, new object[] { 5 })); // 범위 밖 슬롯 저장 차단
            Assert.IsFalse((bool)serviceType.GetMethod("Delete").Invoke(service, new object[] { -1 })); // 범위 밖 슬롯 삭제 차단
            Assert.IsTrue((bool)serviceType.GetMethod("SaveLobby").Invoke(service, new object[] { 0 })); // 자동 거점 슬롯 저장 확인
            File.WriteAllText(Path.Combine(directory, "auto-run.json"), "{\"version\":1}"); // 손상된 자동 탐사 파일 준비
            Assert.IsTrue((bool)serviceType.GetMethod("CanLoad").Invoke(service, new object[] { 0 })); // 유효한 자동 거점 파일 선택 확인
            Assert.IsFalse((bool)serviceType.GetMethod("IsExploration").Invoke(service, new object[] { 0 })); // 손상된 탐사 파일 제외 확인
            Assert.IsTrue((bool)serviceType.GetMethod("Delete").Invoke(service, new object[] { 0 })); // 자동 슬롯 삭제 확인
            Assert.IsFalse(File.Exists(Path.Combine(directory, "auto-lobby.json"))); // 자동 거점 파일 삭제 확인
            Assert.IsFalse(File.Exists(Path.Combine(directory, "auto-run.json"))); // 손상된 자동 탐사 파일 삭제 확인
            Assert.IsTrue((bool)serviceType.GetMethod("CanLoad").Invoke(service, new object[] { 2 })); // 수동 슬롯 유지 확인
        } // 슬롯 검사 종료
        finally // 테스트 파일 정리
        { // 임시 폴더 정리 시작
            if (Directory.Exists(directory)) // 테스트 폴더 존재 확인
            { // 테스트 폴더 제거 시작
                Directory.Delete(directory, true); // 독립 테스트 폴더 제거
            } // 테스트 폴더 제거 종료
        } // 임시 폴더 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 수동 슬롯 검증 종료
} // 저장 슬롯 테스트 종료
