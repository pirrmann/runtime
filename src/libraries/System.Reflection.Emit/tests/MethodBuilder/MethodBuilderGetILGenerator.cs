// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Runtime.Interop;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetILGenerator
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [InlineData(20)]
        [InlineData(-10)]
        public void GetILGenerator_Int(int size)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            ILGenerator ilGenerator = method.GetILGenerator(size);
            int expectedReturn = 5;
            ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal(expectedReturn, createdMethod.Invoke(null, null));

            // Verify MetadataToken
            Assert.Equal(method.MetadataToken, createdMethod.MetadataToken);
            MethodInfo methodFromToken = (MethodInfo)type.Module.ResolveMethod(method.MetadataToken);
            Assert.Equal(createdMethod, methodFromToken);

            MemberInfo memberInfoFromToken = (MemberInfo)type.Module.ResolveMember(method.MetadataToken);
            Assert.Equal(methodFromToken, memberInfoFromToken);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public | MethodAttributes.PinvokeImpl)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.PinvokeImpl)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)]
        public void GetILGenerator_NoMethodBody_ThrowsInvalidOperationException(TypeAttributes typeAttributes, MethodAttributes methodAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(typeAttributes);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes);

            Assert.Throws<InvalidOperationException>(() => method.GetILGenerator());
            Assert.Throws<InvalidOperationException>(() => method.GetILGenerator(10));
        }

        [Theory]
        [InlineData(MethodAttributes.Abstract)]
        [InlineData(MethodAttributes.Assembly)]
        [InlineData(MethodAttributes.CheckAccessOnOverride)]
        [InlineData(MethodAttributes.FamANDAssem)]
        [InlineData(MethodAttributes.Family)]
        [InlineData(MethodAttributes.FamORAssem)]
        [InlineData(MethodAttributes.Final)]
        [InlineData(MethodAttributes.HasSecurity)]
        [InlineData(MethodAttributes.HideBySig)]
        [InlineData(MethodAttributes.MemberAccessMask)]
        [InlineData(MethodAttributes.NewSlot)]
        [InlineData(MethodAttributes.Private)]
        [InlineData(MethodAttributes.Public)]
        [InlineData(MethodAttributes.RequireSecObject)]
        [InlineData(MethodAttributes.ReuseSlot)]
        [InlineData(MethodAttributes.RTSpecialName)]
        [InlineData(MethodAttributes.SpecialName)]
        [InlineData(MethodAttributes.Static)]
        [InlineData(MethodAttributes.UnmanagedExport)]
        [InlineData(MethodAttributes.Virtual)]
        [InlineData(MethodAttributes.Assembly | MethodAttributes.CheckAccessOnOverride |
                MethodAttributes.FamORAssem | MethodAttributes.Final |
                MethodAttributes.HasSecurity | MethodAttributes.HideBySig | MethodAttributes.MemberAccessMask |
                MethodAttributes.NewSlot | MethodAttributes.Private |
                MethodAttributes.PrivateScope | MethodAttributes.RequireSecObject |
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName |
                MethodAttributes.Static | MethodAttributes.UnmanagedExport)]
        public void GetILGenerator_DifferentAttributes(MethodAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod(attributes.ToString(), attributes);
            Assert.NotNull(method.GetILGenerator());
        }

        [Fact]
        public void LoadPointerTypeInILGeneratedMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type pointerType = type.MakePointerType();

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
            ILGenerator ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldtoken, pointerType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("get_FullName"));
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal("TestType*", createdMethod.Invoke(null, null));
        }

        [Fact]
        public void LoadArrayTypeInILGeneratedMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type arrayType = type.MakeArrayType();

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
            ILGenerator ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldtoken, arrayType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("get_FullName"));
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal("TestType[]", createdMethod.Invoke(null, null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/82257", TestRuntimes.Mono)]
        public void LoadByRefTypeInILGeneratedMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type byrefType = type.MakeByRefType();

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
            ILGenerator ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldtoken, byrefType);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("get_FullName"));
            ilGenerator.Emit(OpCodes.Ret);

            Type createdType = type.CreateType();
            MethodInfo createdMethod = createdType.GetMethod("TestMethod");
            Assert.Equal("TestType&", createdMethod.Invoke(null, null));
        }
    }
}
