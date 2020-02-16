using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Buffers.Binary;

namespace Noise
{
    /// <summary>
	/// A pair of <see href="https://noiseprotocol.org/noise.html#the-cipherstate-object">CipherState</see>
	/// objects for encrypting transport messages.
    /// This implementation is not threadsafe!
	/// </summary>
    internal sealed class OOTransport<CipherType> : Transport where CipherType : Cipher, new()
    {
        private readonly bool initiator;
        private readonly CipherState<CipherType> c1;
        private readonly CipherState<CipherType> c2;
        private bool disposed;
        private const int nonceSize = sizeof(ulong);
        private ReplayFilter filter;

        public OOTransport(bool initiator, CipherState<CipherType> c1, CipherState<CipherType> c2)
        {
            Exceptions.ThrowIfNull(c1, nameof(c1));
            this.initiator = initiator;
            this.c1 = c1;
            this.c2 = c2;
            filter = new ReplayFilterNoBitshift();
        }

        public bool IsOneWay
        {
            get
            {
                Exceptions.ThrowIfDisposed(disposed, nameof(Transport<CipherType>));
                return c2 == null;
            }
        }

        public int WriteMessage(ReadOnlySpan<byte> payload, Span<byte> messageBuffer)
        {
            Exceptions.ThrowIfDisposed(disposed, nameof(Transport<CipherType>));

            if (!initiator && IsOneWay)
            {
                throw new InvalidOperationException("Responder cannot write messages to a one-way stream.");
            }

            if (payload.Length + Aead.TagSize + sizeof(uint) > Protocol.MaxMessageLength)
            {
                throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MaxMessageLength} bytes in length.");
            }

            if (payload.Length + Aead.TagSize + sizeof(uint) > messageBuffer.Length)
            {
                throw new ArgumentException("Message buffer does not have enough space to hold the ciphertext.");
            }

            var cipher = initiator ? c1 : c2;
            Debug.Assert(cipher.HasKey());
            
            BinaryPrimitives.WriteUInt64BigEndian(messageBuffer, cipher.GetNonce());
            
            return cipher.EncryptWithAd(null, payload, messageBuffer.Slice(nonceSize)) + nonceSize;
        }

        public int ReadMessage(ReadOnlySpan<byte> message, Span<byte> payloadBuffer)
        {
            Exceptions.ThrowIfDisposed(disposed, nameof(Transport<CipherType>));

            if (initiator && IsOneWay)
            {
                throw new InvalidOperationException("Initiator cannot read messages from a one-way stream.");
            }

            if (message.Length > Protocol.MaxMessageLength)
            {
                throw new ArgumentException($"Noise message must be less than or equal to {Protocol.MaxMessageLength} bytes in length.");
            }

            if (message.Length < Aead.TagSize)
            {
                throw new ArgumentException($"Noise message must be greater than or equal to {Aead.TagSize} bytes in length.");
            }

            if (message.Length - Aead.TagSize > payloadBuffer.Length)
            {
                throw new ArgumentException("Payload buffer does not have enough space to hold the plaintext.");
            }

            var cipher = initiator ? c2 : c1;
            Debug.Assert(cipher.HasKey());

            var n = BinaryPrimitives.ReadUInt64BigEndian(message);
            if (filter.ValidateCounter(n))
                cipher.SetNonce(n);
            else throw new OverflowException("Nonce is out of replay filter window value, or reuse is attempted!");

            return cipher.DecryptWithAd(null, message.Slice(nonceSize), payloadBuffer);
        }

        public void RekeyInitiatorToResponder()
        {
            Exceptions.ThrowIfDisposed(disposed, nameof(Transport<CipherType>));

            c1.Rekey();
        }

        public void RekeyResponderToInitiator()
        {
            Exceptions.ThrowIfDisposed(disposed, nameof(Transport<CipherType>));

            if (IsOneWay)
            {
                throw new InvalidOperationException("Cannot rekey responder to initiator in a one-way stream.");
            }

            c2.Rekey();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                c1.Dispose();
                c2?.Dispose();
                disposed = true;
            }
        }
    }
}
