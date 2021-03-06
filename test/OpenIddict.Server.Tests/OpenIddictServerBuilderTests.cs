using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace OpenIddict.Server.Tests
{
    public class OpenIddictServerBuilderTests
    {
        [Fact]
        public void Constructor_ThrowsAnExceptionForNullServices()
        {
            // Arrange
            var services = (IServiceCollection)null;

            // Act and assert
            var exception = Assert.Throws<ArgumentNullException>(() => new OpenIddictServerBuilder(services));

            Assert.Equal("services", exception.ParamName);
        }

        [Fact]
        public void AddEventHandler_ThrowsAnExceptionWhenConfigurationIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Action<OpenIddictServerHandlerDescriptor.Builder<CustomContext>> configuration = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.AddEventHandler(configuration));
            Assert.Equal(nameof(configuration), exception.ParamName);
        }

        [Fact]
        public void AddEventHandler_ThrowsAnExceptionWhenDescriptorIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            OpenIddictServerHandlerDescriptor descriptor = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.AddEventHandler(descriptor));
            Assert.Equal(nameof(descriptor), exception.ParamName);
        }

        [Fact]
        public void AddEventHandler_HandlerIsAttached()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEventHandler<CustomContext>(x =>
            {
                x.UseSingletonHandler<CustomHandler>();
            });

            // Assert
            Assert.Contains(services, service => service.ServiceType == typeof(CustomHandler));
        }

        [Fact]
        public void AddEventHandler_HandlerInstanceIsRegistered()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEventHandler<CustomContext>(x =>
            {
                x.UseSingletonHandler(new CustomHandler());
            });

            // Assert
            Assert.Contains(services, service =>
                service.ServiceType == typeof(CustomHandler) &&
                service.ImplementationInstance?.GetType() == typeof(CustomHandler) &&
                service.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddEventHandler_SingletonHandlerIsRegisteredAsASingleton()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEventHandler<CustomContext>(x =>
            {
                x.UseSingletonHandler<CustomHandler>();
            });

            // Assert
            Assert.Contains(services, service =>
                service.ServiceType == typeof(CustomHandler) &&
                service.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddEventHandler_ScopedHandlerIsRegisteredAsScoped()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEventHandler<CustomContext>(x =>
            {
                x.UseScopedHandler<CustomHandler>();
            });

            // Assert
            Assert.Contains(services, service =>
                service.ServiceType == typeof(CustomHandler) &&
                service.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddEncryptionCredentials_ThrowsExceptionWhenCredentialsAreNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            EncryptingCredentials credentials = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.AddEncryptionCredentials(credentials));
            Assert.Equal(nameof(credentials), exception.ParamName);
        }

        [Fact]
        public void AddEncryptionKey_ThrowsExceptionWhenKeyIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            SecurityKey key = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.AddEncryptionKey(key));
            Assert.Equal(nameof(key), exception.ParamName);
        }

        [Fact]
        public void AddEncryptionKey_ThrowsExceptionWhenAsymmetricKeyPrivateKeyIsMissing()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            var key = new Mock<AsymmetricSecurityKey>();
            key.SetupGet(x => x.PrivateKeyStatus).Returns(PrivateKeyStatus.DoesNotExist); 

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => builder.AddEncryptionKey(key.Object));
            Assert.Equal("The asymmetric encryption key doesn't contain the required private key.", exception.Message);
        }

        [Fact]
        public void RemoveEventHandler_ThrowsAnExceptionWhenDescriptorIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            OpenIddictServerHandlerDescriptor descriptor = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.RemoveEventHandler(descriptor));
            Assert.Equal(nameof(descriptor), exception.ParamName);
        }

        [Fact]
        public void RemoveEventHandler_RemovesService()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            OpenIddictServerHandlerDescriptor descriptor = OpenIddictServerHandlerDescriptor.CreateBuilder<CustomContext>().UseSingletonHandler<CustomHandler>().Build();
            builder.AddEventHandler(descriptor);

            // Act
            builder.RemoveEventHandler(descriptor);
            var options = GetOptions(services);

            // Assert
            Assert.DoesNotContain(services, x => x.ServiceType == descriptor.ServiceDescriptor.ServiceType);
            Assert.DoesNotContain(options.CustomHandlers, x => x.ServiceDescriptor.ServiceType == descriptor.ServiceDescriptor.ServiceType);
            Assert.DoesNotContain(options.DefaultHandlers, x => x.ServiceDescriptor.ServiceType == descriptor.ServiceDescriptor.ServiceType);
        }

        [Fact]
        public void Configure_OptionsAreCorrectlyAmended()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.Configure(configuration => configuration.AccessTokenLifetime = TimeSpan.FromDays(1));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromDays(1), options.AccessTokenLifetime);
        }

        [Fact]
        public void Configure_ThrowsAnExceptionWhenConfigurationIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Action<OpenIddictServerOptions> configuration = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.Configure(configuration));
            Assert.Equal(nameof(configuration), exception.ParamName);
        }

        [Fact]
        public void AddDevelopmentSigningCertificate_ThrowsAnExceptionForNullSubject()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act and assert
            var exception = Assert.Throws<ArgumentNullException>(delegate
            {
                builder.AddDevelopmentSigningCertificate(subject: null);
            });

            Assert.Equal("subject", exception.ParamName);
        }

        [Fact]
        public void AddDevelopmentEncryptionCertificate_ThrowsAnExceptionForNullSubject()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            X500DistinguishedName subject = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.AddDevelopmentEncryptionCertificate(subject));
            Assert.Equal(nameof(subject), exception.ParamName);
        }

#if SUPPORTS_CERTIFICATE_GENERATION
        [Fact]
        public void AddDevelopmentSigningCertificate_CanGenerateCertificate()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddDevelopmentSigningCertificate();

            var options = GetOptions(services);

            // Assert
            Assert.Equal(1, options.SigningCredentials.Count);
            Assert.Equal(SecurityAlgorithms.RsaSha256, options.SigningCredentials[0].Algorithm);
            Assert.NotNull(options.SigningCredentials[0].Kid);
        }
#else
        [Fact]
        public void AddDevelopmentSigningCertificate_ThrowsAnExceptionOnUnsupportedPlatforms()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act and assert
            var exception = Assert.Throws<PlatformNotSupportedException>(delegate
            {
                builder.AddDevelopmentSigningCertificate();
                return GetOptions(services);
            });

            Assert.Equal("X.509 certificate generation is not supported on this platform.", exception.Message);
        }
#endif

        [Fact]
        public void AddEphemeralSigningKey_SigningKeyIsCorrectlyAdded()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEphemeralSigningKey();

            var options = GetOptions(services);

            // Assert
            Assert.Equal(1, options.SigningCredentials.Count);
        }

        [Theory]
        [InlineData(SecurityAlgorithms.RsaSha256)]
        [InlineData(SecurityAlgorithms.RsaSha384)]
        [InlineData(SecurityAlgorithms.RsaSha512)]
#if SUPPORTS_ECDSA
        [InlineData(SecurityAlgorithms.EcdsaSha256)]
        [InlineData(SecurityAlgorithms.EcdsaSha384)]
        [InlineData(SecurityAlgorithms.EcdsaSha512)]
#endif
        public void AddEphemeralSigningKey_SigningCredentialsUseSpecifiedAlgorithm(string algorithm)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddEphemeralSigningKey(algorithm);

            var options = GetOptions(services);
            var credentials = options.SigningCredentials[0];

            // Assert
            Assert.Equal(algorithm, credentials.Algorithm);
        }

        //[Fact]
        //public void AddEncryptingKey_EncryptingKeyIsCorrectlyAdded()
        //{
        //    // Arrange
        //    var services = CreateServices();
        //    var builder = CreateBuilder(services);

        //    var factory = Mock.Of<CryptoProviderFactory>(mock =>
        //        mock.IsSupportedAlgorithm(SecurityAlgorithms.Aes256KW, It.IsAny<SecurityKey>()));

        //    var key = Mock.Of<SecurityKey>(mock => mock.CryptoProviderFactory == factory);

        //    // Act
        //    builder.AddEncryptingKey(key);

        //    var options = GetOptions(services);

        //    // Assert
        //    Assert.Same(key, options.EncryptingCredentials[0].Key);
        //}

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256)]
        [InlineData(SecurityAlgorithms.RsaSha256)]
#if SUPPORTS_ECDSA
        [InlineData(SecurityAlgorithms.EcdsaSha256)]
        [InlineData(SecurityAlgorithms.EcdsaSha384)]
        [InlineData(SecurityAlgorithms.EcdsaSha512)]
#endif
        public void AddSigningKey_SigningKeyIsCorrectlyAdded(string algorithm)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            var factory = Mock.Of<CryptoProviderFactory>(mock =>
                mock.IsSupportedAlgorithm(algorithm, It.IsAny<SecurityKey>()));

            var key = Mock.Of<SecurityKey>(mock => mock.CryptoProviderFactory == factory);

            // Act
            builder.AddSigningKey(key);

            var options = GetOptions(services);

            // Assert
            Assert.Same(key, options.SigningCredentials[0].Key);
        }

        [Fact]
        public void AddSigningCertificate_SigningKeyIsCorrectlyAdded()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AddSigningCertificate(
                assembly: typeof(OpenIddictServerBuilderTests).GetTypeInfo().Assembly,
                resource: "OpenIddict.Server.Tests.Certificate.pfx",
                password: "OpenIddict");

            var options = GetOptions(services);

            // Assert
            Assert.IsType<X509SecurityKey>(options.SigningCredentials[0].Key);
        }

        [Fact]
        public void AllowAuthorizationCodeFlow_CodeFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowAuthorizationCodeFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode, options.GrantTypes);
        }

        [Fact]
        public void AllowClientCredentialsFlow_ClientCredentialsFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowClientCredentialsFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIddictConstants.GrantTypes.ClientCredentials, options.GrantTypes);
        }

        [Fact]
        public void AllowCustomFlow_CustomFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowCustomFlow("urn:ietf:params:oauth:grant-type:custom_grant");

            var options = GetOptions(services);

            // Assert
            Assert.Contains("urn:ietf:params:oauth:grant-type:custom_grant", options.GrantTypes);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AllowCustomFlow_ThrowsAnExceptionForType(string type)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.AllowCustomFlow(type));
            Assert.Equal(nameof(type), exception.ParamName);
            Assert.Contains("The grant type cannot be null or empty.", exception.Message);
        }

        [Fact]
        public void AllowImplicitFlow_ImplicitFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowImplicitFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIddictConstants.GrantTypes.Implicit, options.GrantTypes);
        }

        [Fact]
        public void AllowPasswordFlow_PasswordFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowPasswordFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIddictConstants.GrantTypes.Password, options.GrantTypes);
        }

        [Fact]
        public void AllowRefreshTokenFlow_RefreshTokenFlowIsAddedToGrantTypes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowRefreshTokenFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIddictConstants.GrantTypes.RefreshToken, options.GrantTypes);
        }

        [Fact]
        public void DisableAuthorizationStorage_AuthorizationStorageIsDisabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.DisableAuthorizationStorage();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableAuthorizationStorage);
        }

        [Fact]
        public void SetConfigurationEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetConfigurationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetConfigurationEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetConfigurationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        public const string InvalidUriString = @"C:\";

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetConfigurationEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetConfigurationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetConfigurationEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetConfigurationEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.ConfigurationEndpointUris);
        }

        [Fact]
        public void SetConfigurationEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetConfigurationEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.ConfigurationEndpointUris);
        }

        [Fact]
        public void SetDeviceEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetDeviceEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetDeviceEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetDeviceEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetDeviceEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetDeviceEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetDeviceEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetDeviceEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.DeviceEndpointUris);
        }

        [Fact]
        public void SetDeviceEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetDeviceEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.DeviceEndpointUris);
        }

        [Fact]
        public void AddDeviceCodeFlow_AddsDeviceCodeGrantType()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AllowDeviceCodeFlow();

            var options = GetOptions(services);

            // Assert
            Assert.Contains(OpenIddictConstants.GrantTypes.DeviceCode, options.GrantTypes);
        }

        [Fact]
        public void SetCryptographyEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetCryptographyEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetCryptographyEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetCryptographyEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetCryptographyEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetCryptographyEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetCryptographyEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetCryptographyEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.CryptographyEndpointUris);
        }

        [Fact]
        public void SetCryptographyEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetCryptographyEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.CryptographyEndpointUris);
        }

        [Fact]
        public void DisableSlidingExpiration_SlidingExpirationIsDisabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.DisableSlidingExpiration();

            var options = GetOptions(services);

            // Assert
            Assert.False(options.UseSlidingExpiration);
        }

        [Fact]
        public void DisableTokenStorage_TokenStorageIsDisabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.DisableTokenStorage();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableTokenStorage);
        }

        [Fact]
        public void DisableAccessTokenEncryption_AccessTokenEncryptionIsDisabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.DisableAccessTokenEncryption();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableAccessTokenEncryption);
        }
        
        [Fact]
        public void SetAuthorizationEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetAuthorizationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetAuthorizationEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetAuthorizationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetAuthorizationEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetAuthorizationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetAuthorizationEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetAuthorizationEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.AuthorizationEndpointUris);
        }

        [Fact]
        public void SetAuthorizationEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetAuthorizationEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.AuthorizationEndpointUris);
        }
        
        [Fact]
        public void SetIntrospectionEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetIntrospectionEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetIntrospectionEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetIntrospectionEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetIntrospectionEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetIntrospectionEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetIntrospectionEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetIntrospectionEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.IntrospectionEndpointUris);
        }

        [Fact]
        public void SetIntrospectionEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetIntrospectionEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.IntrospectionEndpointUris);
        }

        [Fact]
        public void SetLogoutEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetLogoutEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetLogoutEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetLogoutEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetLogoutEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetLogoutEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetLogoutEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetLogoutEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.LogoutEndpointUris);
        }

        [Fact]
        public void SetLogoutEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetLogoutEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.LogoutEndpointUris);
        }

        //[Fact]
        //public void EnableRequestCaching_RequestCachingIsEnabled()
        //{
        //    // Arrange
        //    var services = CreateServices();
        //    var builder = CreateBuilder(services);

        //    // Act
        //    builder.EnableRequestCaching();

        //    var options = GetOptions(services);

        //    // Assert
        //    Assert.True(options.EnableRequestCaching);
        //}

        [Fact]
        public void SetRevocationEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetRevocationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetRevocationEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetRevocationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetRevocationEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetRevocationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetRevocationEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetRevocationEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.RevocationEndpointUris);
        }

        [Fact]
        public void SetRevocationEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetRevocationEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.RevocationEndpointUris);
        }

        [Fact]
        public void DisableScopeValidation_ScopeValidationIsDisabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.DisableScopeValidation();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.DisableScopeValidation);
        }

        [Fact]
        public void SetTokenEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetTokenEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetTokenEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetTokenEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetTokenEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetTokenEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetTokenEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetTokenEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.TokenEndpointUris);
        }

        [Fact]
        public void SetTokenEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetTokenEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.TokenEndpointUris);
        }

        [Fact]
        public void SetUserinfoEndpointUris_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetUserinfoEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetUserinfoEndpointUris_Strings_ThrowsExceptionWhenAddressesIsNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetUserinfoEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetUserinfoEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = {new Uri(uri), };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetUserinfoEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetUserinfoEndpointUris_ClearsUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetUserinfoEndpointUris(Array.Empty<Uri>());

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.UserinfoEndpointUris);
        }

        [Fact]
        public void SetUserinfoEndpointUris_AddsUri()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetUserinfoEndpointUris("http://localhost/endpoint-path");

            var options = GetOptions(services);

            // Assert
            Assert.Contains(new Uri("http://localhost/endpoint-path"), options.UserinfoEndpointUris);
        }

        [Fact]
        public void AcceptAnonymousClients_ClientIdentificationIsOptional()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.AcceptAnonymousClients();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.AcceptAnonymousClients);
        }

        [Fact]
        public void SetAccessTokenLifetime_DefaultAccessTokenLifetimeIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetAccessTokenLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.AccessTokenLifetime);
        }

        [Fact]
        public void SetAccessTokenLifetime_AccessTokenLifetimeCanBeSetToNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetAccessTokenLifetime(null);

            var options = GetOptions(services);

            // Assert
            Assert.Null(options.AccessTokenLifetime);
        }

        [Fact]
        public void SetAuthorizationCodeLifetime_DefaultAuthorizationCodeLifetimeIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.AuthorizationCodeLifetime);
        }

        [Fact]
        public void SetAuthorizationCodeLifetime_AuthorizationCodeLifetimeCanBeSetToNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetAuthorizationCodeLifetime(null);

            var options = GetOptions(services);

            // Assert
            Assert.Null(options.AuthorizationCodeLifetime);
        }

        [Fact]
        public void SetIdentityTokenLifetime_DefaultIdentityTokenLifetimeIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetIdentityTokenLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.IdentityTokenLifetime);
        }

        [Fact]
        public void SetIdentityTokenLifetime_IdentityTokenLifetimeCanBeSetToNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetIdentityTokenLifetime(null);

            var options = GetOptions(services);

            // Assert
            Assert.Null(options.IdentityTokenLifetime);
        }

        [Fact]
        public void SetDeviceCodeLifetimeLifetime_DefaultDeviceCodeLifetimeIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetDeviceCodeLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.DeviceCodeLifetime);
        }

        [Fact]
        public void SetDeviceCodeLifetimeLifetime_DeviceCodeLifetimeCanBeSetToNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetDeviceCodeLifetime(null);

            var options = GetOptions(services);

            // Assert
            Assert.Null(options.DeviceCodeLifetime);
        }

        [Fact]
        public void SetUserCodeLifetime_DefaultUserCodeLifetimeIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetUserCodeLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.UserCodeLifetime);
        }

        [Fact]
        public void SetUserCodeLifetime_UserLifetimeCanBeSetToNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetUserCodeLifetime(null);

            var options = GetOptions(services);

            // Assert
            Assert.Null(options.UserCodeLifetime);
        }

        [Fact]
        public void SetRefreshTokenLifetime_DefaultRefreshTokenLifetimeIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetRefreshTokenLifetime(TimeSpan.FromMinutes(42));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(42), options.RefreshTokenLifetime);
        }

        [Fact]
        public void SetRefreshTokenLifetime_RefreshTokenLifetimeCanBeSetToNull()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetRefreshTokenLifetime(null);

            var options = GetOptions(services);

            // Assert
            Assert.Null(options.RefreshTokenLifetime);
        }

        //[Fact]
        //public void SetRequestCachingPolicy_ThrowsAnExceptionForNullPolicy()
        //{
        //    // Arrange
        //    var services = CreateServices();
        //    var builder = CreateBuilder(services);

        //    // Act and assert
        //    var exception = Assert.Throws<ArgumentNullException>(() => builder.SetRequestCachingPolicy(null));

        //    Assert.Equal("policy", exception.ParamName);
        //}

        //[Fact]
        //public void SetRequestCachingPolicy_PolicyIsUpdated()
        //{
        //    // Arrange
        //    var services = CreateServices();
        //    var builder = CreateBuilder(services);

        //    var policy = new DistributedCacheEntryOptions
        //    {
        //        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(42),
        //        SlidingExpiration = TimeSpan.FromSeconds(42)
        //    };

        //    // Act
        //    builder.SetRequestCachingPolicy(policy);

        //    var options = GetOptions(services);

        //    // Assert
        //    Assert.Same(policy, options.RequestCachingPolicy);
        //}

        //[Fact]
        //public void UseDataProtectionProvider_DefaultProviderIsReplaced()
        //{
        //    // Arrange
        //    var services = CreateServices();
        //    var builder = CreateBuilder(services);

        //    // Act
        //    builder.UseDataProtectionProvider(new EphemeralDataProtectionProvider());

        //    var options = GetOptions(services);

        //    // Assert
        //    Assert.IsType<EphemeralDataProtectionProvider>(options.DataProtectionProvider);
        //}

        //[Fact]
        //public void UseJsonWebTokens_AccessTokenHandlerIsCorrectlySet()
        //{
        //    // Arrange
        //    var services = CreateServices();
        //    var builder = CreateBuilder(services);

        //    // Act
        //    builder.UseJsonWebTokens();

        //    var options = GetOptions(services);

        //    // Assert
        //    Assert.IsType<JwtSecurityTokenHandler>(options.AccessTokenHandler);
        //}

        [Fact]
        public void SetIssuer_ThrowsAnExceptionForNullIssuer()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act and assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetIssuer(null));

            Assert.Equal("address", exception.ParamName);
        }

        [Fact]
        public void SetIssuer_AddressIsReplaced()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.SetIssuer(new Uri("http://www.fabrikam.com/"));

            var options = GetOptions(services);

            // Assert
            Assert.Equal(new Uri("http://www.fabrikam.com/"), options.Issuer);
        }

        [Fact]
        public void RegisterClaims_ClaimsAreAdded()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.RegisterClaims("custom_claim_1", "custom_claim_2");

            var options = GetOptions(services);

            // Assert
            Assert.Contains("custom_claim_1", options.Claims);
            Assert.Contains("custom_claim_2", options.Claims);
        }

        [Fact]
        public void RegisterClaims_ThrowsAnExceptionForNullClaims()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] claims = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.RegisterClaims(claims));
            Assert.Equal(nameof(claims), exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RegisterClaims_ThrowsAnExceptionForClaim(string claim)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] claims = { claim };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.RegisterClaims(claims));
            Assert.Equal(nameof(claims), exception.ParamName);
            Assert.Contains("Claims cannot be null or empty.", exception.Message);
        }

        [Fact]
        public void RegisterScopes_ScopesAreAdded()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.RegisterScopes("custom_scope_1", "custom_scope_2");

            var options = GetOptions(services);

            // Assert
            Assert.Contains("custom_scope_1", options.Scopes);
            Assert.Contains("custom_scope_2", options.Scopes);
        }

        [Fact]
        public void RegisterScopes_ThrowsAnExceptionForNullScopes()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] scopes = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.RegisterScopes(scopes));
            Assert.Equal(nameof(scopes), exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RegisterScopes_ThrowsAnExceptionForScope(string scope)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] scopes = { scope };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.RegisterScopes(scopes));
            Assert.Equal(nameof(scopes), exception.ParamName);
            Assert.Contains("Scopes cannot be null or empty.", exception.Message);
        }

        [Fact]
        public void UseReferenceTokens_ReferenceTokensAreEnabled()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);

            // Act
            builder.UseReferenceTokens();

            var options = GetOptions(services);

            // Assert
            Assert.True(options.UseReferenceTokens);
        }

        [Fact]
        public void SetVerificationEndpointUris_ThrowsExceptionWhenNullAddresses()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetVerificationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetVerificationEndpointUris_Strings_ThrowsExceptionWhenNullAddresses()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => builder.SetVerificationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
        }

        [Fact]
        public void SetVerificationEndpointUris_Strings_AddedUriIsRelativeOrAbsoluteUriKind()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            string[] addresses = {"http://localhost/verify"};

            // Act
            builder.SetVerificationEndpointUris(addresses);

            var options = GetOptions(services);

            // Assert
            Assert.True(options.VerificationEndpointUris[0].IsAbsoluteUri);
        }

        [Theory]
        [InlineData(InvalidUriString)]
        public void SetVerificationEndpointUris_ThrowsExceptionForUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri)};

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => builder.SetVerificationEndpointUris(addresses));
            Assert.Equal(nameof(addresses), exception.ParamName);
            Assert.Contains("One of the specified addresses is not valid.", exception.Message);
        }

        [Fact]
        public void SetVerificationEndpointUris_ClearsExistingUris()
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = Array.Empty<Uri>();

            // Act
            builder.SetVerificationEndpointUris(addresses);

            var options = GetOptions(services);

            // Assert
            Assert.Empty(options.VerificationEndpointUris);
        }

        [Theory]
        [InlineData("http://localhost/verify")]
        [InlineData("http://localhost/verify-1")]
        [InlineData("http://localhost/verification")]
        [InlineData("http://localhost/verification-1")]
        public void SetVerificationEndpointUris_AddsUri(string uri)
        {
            // Arrange
            var services = CreateServices();
            var builder = CreateBuilder(services);
            Uri[] addresses = { new Uri(uri), };

            // Act
            builder.SetVerificationEndpointUris(addresses);

            var options = GetOptions(services);

            // Assert
            Assert.Contains(addresses[0], options.VerificationEndpointUris);
        }

        private static IServiceCollection CreateServices()
        {
            return new ServiceCollection().AddOptions();
        }

        private static OpenIddictServerBuilder CreateBuilder(IServiceCollection services)
            => new OpenIddictServerBuilder(services);

        private static OpenIddictServerOptions GetOptions(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<OpenIddictServerOptions>>();
            //return options.Get(OpenIddictServerDefaults.AuthenticationScheme);
            return options.Value;
        }

        private class CustomContext : BaseContext
        {
            /// <summary>
            /// Creates a new instance of the <see cref="BaseContext"/> class.
            /// </summary>
            public CustomContext(OpenIddictServerTransaction transaction) : base(transaction) { }
        }

        private class CustomHandler : IOpenIddictServerHandler<CustomContext>
        {
            /// <summary>
            /// Processes the event.
            /// </summary>
            /// <param name="context">The context associated with the event to process.</param>
            /// <returns>
            /// A <see cref="ValueTask"/> that can be used to monitor the asynchronous operation.
            /// </returns>
            public ValueTask HandleAsync(CustomContext context)
            {
                return new ValueTask();
            }
        }
    }
}