using System;

namespace Core
{
    /*
     * IDisposable :
     * .NET Framework에서 새로운 클래스를 만들 때,
     * 한시적으로 사용할 자원들을 묶어 관리하기 위한 패턴
     */
    public interface IBlock : IDisposable
    {
        // Id of the block, must be unique
        uint Id { get; }

        // A block may contain one or more header metadata,
        // each header identified by a number and 8 bytes value.
        long GetHeader(int field);

        // Change the value of specified header.
        // Data must not be written to disk until the block is disposed.
        void SetHeader(int field, long value);

        // Read content of this block (src) into given buffer (dst)
        // src : source. 데이터 원본, 출발점. => 여기서는 block 자체. 저장된 데이터 있는 곳.
        // srcOffset : block내에서 읽기 시작할 위치.
        // dst : destination. 데이터 목적지, 도착점. => 데이터 저장할 외부 버퍼.
        // dstOffset : 외부 버퍼에서 데이터를 쓰기 시작할 위치.
        // count : 읽어올 바이트의 개수. 한 번에 읽는 데이터의 양이 약 2GB를 넘을 것 같다면, long으로 확장. => 기존 RDBMS는 일반적으로 4~8KB, 최대는 16MB(MongoDB) 단위로 동작함. 따라서 대용량 데이터를 위한 시스템 구축이 아니라면 int 단위를 벗어날 일이 많지 않음.
        void Read(byte[] dst, long dstOffset, long srcOffset, int count);

        // Write content of given buffer (src) into this (dst)
        // src : block에 쓰려는 데이터가 있는 외부 버퍼.
        // srcOffset : 외부 버퍼에서 읽기 시작할 위치.
        // dst : block 자체. 데이터가 쓰여질 곳.
        // dstOffset : block 내에서 쓰기 시작할 위치.
        void Write(byte[] src, long srcOffset, long dstOffset, int count);
    }
}
